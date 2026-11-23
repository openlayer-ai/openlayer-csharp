using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Openlayer.Api;

namespace Openlayer
{
    /// <summary>Options for <see cref="OpenlayerClient"/>. Every value is optional.</summary>
    public sealed class OpenlayerClientOptions
    {
        /// <summary>API key. Defaults to the <c>OPENLAYER_API_KEY</c> environment variable.</summary>
        public string? ApiKey { get; set; }

        /// <summary>Base URL of the API. Defaults to <c>OPENLAYER_BASE_URL</c>, then <c>https://api.openlayer.com/v1</c>.</summary>
        public string? BaseUrl { get; set; }

        /// <summary>Request timeout. Defaults to 60 seconds, like the other Openlayer SDKs.</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Custom <see cref="HttpClient"/>. When it is not set, the client uses Kiota's default
        /// handler chain (retry with backoff on 429/503/504, redirects, compression).
        /// </summary>
        public HttpClient? HttpClient { get; set; }
    }

    /// <summary>
    /// Entry point of the Openlayer C# SDK.
    /// <code>
    /// var client = new OpenlayerClient();   // reads OPENLAYER_API_KEY
    /// var projects = await client.Projects.GetAsync();
    /// </code>
    /// The resource tree (<c>Projects</c>, <c>InferencePipelines</c>, ...) is generated from the
    /// OpenAPI spec into <c>src/Openlayer/Generated</c>; this class only adds configuration.
    /// </summary>
    public class OpenlayerClient : OpenlayerApiClient
    {
        /// <summary>The default production base URL.</summary>
        public const string DefaultBaseUrl = "https://api.openlayer.com/v1";

        /// <summary>The SDK version, sent in the <c>User-Agent</c> header.</summary>
        public static readonly string Version =
            typeof(OpenlayerClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        /// <summary>Create a client. See <see cref="OpenlayerClientOptions"/> for the defaults.</summary>
        public OpenlayerClient(OpenlayerClientOptions? options = null)
            : base(CreateAdapter(options ?? new OpenlayerClientOptions()))
        {
        }

        /// <summary>Create a client with an API key and the default options.</summary>
        public OpenlayerClient(string apiKey)
            : this(new OpenlayerClientOptions { ApiKey = apiKey })
        {
        }

        private static IRequestAdapter CreateAdapter(OpenlayerClientOptions options)
        {
            var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("OPENLAYER_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new OpenlayerException(
                    "The API key is missing. Set the OPENLAYER_API_KEY environment variable or pass OpenlayerClientOptions.ApiKey.");
            }

            var baseUrl = options.BaseUrl
                ?? Environment.GetEnvironmentVariable("OPENLAYER_BASE_URL")
                ?? DefaultBaseUrl;

            var httpClient = options.HttpClient ?? KiotaClientFactory.Create();
            if (options.HttpClient == null)
            {
                httpClient.Timeout = options.Timeout ?? TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"openlayer-csharp/{Version}");
            }

            return new HttpClientRequestAdapter(new BearerAuthenticationProvider(apiKey!), httpClient: httpClient)
            {
                BaseUrl = baseUrl.TrimEnd('/'),
            };
        }
    }

    /// <summary>Raised for client configuration errors. API errors are <see cref="ApiException"/>.</summary>
    public class OpenlayerException : Exception
    {
        /// <summary>Create the exception.</summary>
        public OpenlayerException(string message) : base(message) { }
    }

    /// <summary>Sends <c>Authorization: Bearer &lt;key&gt;</c> on every request.</summary>
    internal sealed class BearerAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _apiKey;

        public BearerAuthenticationProvider(string apiKey) => _apiKey = apiKey;

        public Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            request.Headers.TryAdd("Authorization", $"Bearer {_apiKey}");
            return Task.CompletedTask;
        }
    }
}
