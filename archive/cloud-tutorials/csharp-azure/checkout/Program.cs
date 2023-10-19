using System.Text.Json.Serialization;
using Dapr.Client;
using Serilog;

var pubsub = Environment.GetEnvironmentVariable("PUBSUB_NAME") ?? "pubsub";
var topic = Environment.GetEnvironmentVariable("TOPIC") ?? "orders";
var host = Environment.GetEnvironmentVariable("DAPR_RUNTIME_HOST") ?? "http://127.0.0.1";
var grpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog(logger);

var client = new DaprClientBuilder().Build();

var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

app.MapPost("/orders", async (ILoggerFactory loggerFactory, Order order) =>
{

    using var client = new DaprClientBuilder().UseGrpcEndpoint($"{host}:{grpcPort}").Build();
    var logger = loggerFactory.CreateLogger("publish");

    try
    {
        await client.PublishEventAsync(pubsub, topic, order);
        logger.LogInformation($"Successfully published order {order.OrderId} to topic {topic} using {pubsub} connection");
    }
    catch (Exception e)
    {
        logger.LogError("Publishing message failed with error: " + e.Message + e.InnerException?.Message);
    }
}
);

await app.RunAsync();

public record Order([property: JsonPropertyName("orderId")] int OrderId);