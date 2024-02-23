using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var client = new DaprClientBuilder().Build();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

#region Publish Subscribe API 

// Subscribe to messages 
app.MapPost("/pubsub/neworders", (Order order) =>
{
    app.Logger.LogInformation("Order received: {orderId}", order.OrderId);
    return Results.Ok(order);
});

#endregion

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);