// Streams one LLM inference row to an Openlayer inference pipeline.
//
//   export OPENLAYER_API_KEY=...
//   export OPENLAYER_INFERENCE_PIPELINE_ID=...
//   dotnet run --project examples/StreamData
using Openlayer;
using Openlayer.Api.InferencePipelines.Item.DataStream;
using Openlayer.Api.Models;

var pipelineId = Guid.Parse(Environment.GetEnvironmentVariable("OPENLAYER_INFERENCE_PIPELINE_ID")
    ?? throw new InvalidOperationException("Set OPENLAYER_INFERENCE_PIPELINE_ID"));

var client = new OpenlayerClient(); // reads OPENLAYER_API_KEY

var response = await client.InferencePipelines[pipelineId].DataStream.PostAsync(new DataStreamPostRequestBody
{
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
    Rows = new()
    {
        new DataStreamPostRequestBody_rows
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["user_query"] = "what is the meaning of life?",
                ["output"] = "42",
                ["tokens"] = 7,
                ["cost"] = 0.02,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            },
        },
    },
});

Console.WriteLine($"success: {response?.Success}");
