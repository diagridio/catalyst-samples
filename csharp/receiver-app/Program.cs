using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Dapr.Workflow;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

var client = new DaprClientBuilder().Build();

#region Publish Subscribe API 

// Receive messages
app.MapPost("/consume", (Order order) =>
{
    app.Logger.LogInformation($"Message received: {order.OrderId}");
    return Results.Ok();
});

#endregion

#region Request/Reply API 

// Receive invocation request 
app.MapPost("/receiverequest", (Order order) =>
{
    app.Logger.LogInformation("Request received : " + order);
    return Results.Ok(order);
});

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);