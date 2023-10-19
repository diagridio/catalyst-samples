using System.Text.Json.Serialization;
using Dapr;
using Dapr.Client;
using Serilog;

var statestore = Environment.GetEnvironmentVariable("STATESTORE_NAME") ?? "statestore";
var host = Environment.GetEnvironmentVariable("DAPR_RUNTIME_HOST") ?? "http://127.0.0.1";
var grpcPort = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") ?? "50001";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog(logger);

var app = builder.Build();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); }

app.MapPost("/orders", async (ILoggerFactory loggerFactory, Order order) =>
{
    using var client = new DaprClientBuilder().UseGrpcEndpoint($"{host}:{grpcPort}").Build();
    var logger = loggerFactory.CreateLogger("state");

    try
    {
        await client.SaveStateAsync(statestore, order.OrderId.ToString(), order);
        logger.LogInformation($"Order {order.OrderId} successfully persisted");
    }
    catch (Exception e)
    {
        logger.LogError("Persisting order failed with error: " + e.Message + e.InnerException?.Message);
    }

    return Results.Ok(order);
});

await app.RunAsync();

public record Order([property: JsonPropertyName("orderId")] int OrderId);
