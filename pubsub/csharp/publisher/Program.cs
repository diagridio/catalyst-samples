using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var client = new DaprClientBuilder().Build();

var PubSubName = Environment.GetEnvironmentVariable("PUBSUB_NAME") ?? "pubsub";

#region Publish Subscribe API 

// Publish messages 
app.MapPost("/pubsub/orders", async (Order order) =>
{
    // Publish order to Diagrid pubsub, topic: orders 
    try
    {
        await client.PublishEventAsync(PubSubName, "orders", order);
        app.Logger.LogInformation("Publish Successful. Order published: {orderId}", order.OrderId);
        return Results.StatusCode(200);

    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while publishing order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }
});

// Subscribe to messages 
app.MapPost("/pubsub/neworders", (Order order) =>
{
    app.Logger.LogInformation("Order received: {orderId}", order.OrderId);
    return Results.Ok(order);
});

#endregion

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);