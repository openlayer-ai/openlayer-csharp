// Parity harness: makes the same calls as tests/Parity/parity.py (the Python SDK)
// and prints the results as JSON. scripts/parity runs both against a recording
// server and fails if any HTTP request differs; `--live` runs both against a
// real backend and compares the results.
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Openlayer;
using Openlayer.Api.InferencePipelines.Item.DataStream;
using Openlayer.Api.Models;

string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) ?? fallback;
const string DefaultId = "182bd5e5-6e1a-4fe4-a799-aa6d9a6ab26e";
var projectId = Guid.Parse(Env("PROJECT_ID", DefaultId));
var pipelineId = Guid.Parse(Env("PIPELINE_ID", DefaultId));
var workspaceId = Guid.Parse(Env("WORKSPACE_ID", DefaultId));
var projectName = Env("PROJECT_NAME", "csharp-parity");
// scripts/parity sets one TIMESTAMP for both SDKs; the backend rejects rows older than 2 years.
var timestamp = long.Parse(Env("TIMESTAMP", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

var client = new OpenlayerClient(new OpenlayerClientOptions { ApiKey = Env("OPENLAYER_API_KEY", "test-key") });
var result = new SortedDictionary<string, object?>();

async Task Run(string name, Func<Task<object?>> f)
{
    try { result[name] = await f(); }
    catch (ApiException e) { result[name] = $"ERROR {e.ResponseStatusCode}"; }
    catch (Exception e) { result[name] = $"ERROR {e.GetType().Name}"; }
}

await Run("projects.create", async () =>
{
    var r = await client.Projects.PostAsync(new Project { Name = projectName, TaskType = Project_taskType.LlmBase, Description = "SDK parity test" });
    return r?.Name == projectName ? "created" : r?.Name;
});
await Run("projects.list", async () =>
{
    var r = await client.Projects.GetAsync(c => { c.QueryParameters.PerPage = 100; c.QueryParameters.Name = projectName; });
    return r?.Items?.Select(p => p.Name).OrderBy(x => x).ToList();
});
await Run("projects.inference_pipelines.list", async () =>
{
    var r = await client.Projects[projectId].InferencePipelines.GetAsync();
    return r?.Items?.Select(p => p.Name).OrderBy(x => x).ToList();
});
await Run("inference_pipelines.retrieve", async () => (await client.InferencePipelines[pipelineId].GetAsync())?.Name);
await Run("workspaces.retrieve", async () => (await client.Workspaces[workspaceId].GetAsync())?.Name);
await Run("inference_pipelines.data.stream", async () =>
{
    var row = new DataStreamPostRequestBody_rows
    {
        AdditionalData = new Dictionary<string, object>
        {
            ["user_query"] = "what is the meaning of life?",
            ["output"] = "42",
            ["tokens"] = 7,
            ["cost"] = 0.02,
            ["timestamp"] = timestamp,
        },
    };
    var body = new DataStreamPostRequestBody
    {
        Rows = new() { row },
        Config = new DataStreamPostRequestBody.DataStreamPostRequestBody_config
        {
            LLMData = new LLMData
            {
                InputVariableNames = new() { "user_query" },
                OutputColumnName = "output",
                NumOfTokenColumnName = "tokens",
                CostColumnName = "cost",
                TimestampColumnName = "timestamp",
            },
        },
    };
    return (await client.InferencePipelines[pipelineId].DataStream.PostAsync(body))?.Success;
});

Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
