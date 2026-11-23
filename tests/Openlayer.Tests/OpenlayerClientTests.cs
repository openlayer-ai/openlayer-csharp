using System.Net;
using System.Net.Http;
using System.Text;
using Openlayer;
using Xunit;

public class OpenlayerClientTests
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Last;
        public string? Body;
        public string Response = "{\"items\":[{\"name\":\"My Project\"}]}";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private static (OpenlayerClient, RecordingHandler) Create(string? baseUrl = "http://localhost:1/v1")
    {
        var handler = new RecordingHandler();
        var client = new OpenlayerClient(new OpenlayerClientOptions
        {
            ApiKey = "sk-test",
            BaseUrl = baseUrl,
            HttpClient = new HttpClient(handler),
        });
        return (client, handler);
    }

    [Fact]
    public void MissingApiKeyThrows()
    {
        var old = Environment.GetEnvironmentVariable("OPENLAYER_API_KEY");
        Environment.SetEnvironmentVariable("OPENLAYER_API_KEY", null);
        try { Assert.Throws<OpenlayerException>(() => new OpenlayerClient()); }
        finally { Environment.SetEnvironmentVariable("OPENLAYER_API_KEY", old); }
    }

    [Fact]
    public async Task SendsBearerAuthAndUsesBaseUrl()
    {
        var (client, handler) = Create();
        var page = await client.Projects.GetAsync(c => c.QueryParameters.PerPage = 5);

        Assert.Equal("Bearer sk-test", handler.Last!.Headers.Authorization!.ToString());
        Assert.Equal("http://localhost:1/v1/projects?perPage=5", handler.Last.RequestUri!.ToString());
        Assert.Equal("My Project", page!.Items![0].Name);
    }

    [Fact]
    public async Task DefaultsToProductionBaseUrl()
    {
        var old = Environment.GetEnvironmentVariable("OPENLAYER_BASE_URL");
        Environment.SetEnvironmentVariable("OPENLAYER_BASE_URL", null);
        try
        {
            var (client, handler) = Create(baseUrl: null);
            await client.Projects.GetAsync();
            Assert.StartsWith(OpenlayerClient.DefaultBaseUrl + "/projects", handler.Last!.RequestUri!.ToString());
        }
        finally { Environment.SetEnvironmentVariable("OPENLAYER_BASE_URL", old); }
    }
}
