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

// Publish messages 
app.MapPost("/publish", async (Order order) =>
{
    // Publish order to Diagrid pubsub, topic: orders 
    try
    {
        await client.PublishEventAsync("pubsub", "orders", order);
        app.Logger.LogInformation($"Publish Successful. Order published: {order.OrderId}");

    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error occurred while publishing order: {order.OrderId}. {ex.InnerException}");
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Receive messages
app.MapPost("/subscribe", (Order order) =>
{
    app.Logger.LogInformation($"Message received: {order.OrderId}");
    return Results.Ok();
});

#endregion

#region Request/Reply API 

// Invoke another service
app.MapPost("/sendrequest", async (Order order) =>
{
    // Publish order to Diagrid pubsub, topic: orders 
    try
    {
        // Create invoke client for the "invoketarget" App ID
        var httpClient = DaprClient.CreateInvokeHttpClient("invoketarget");

        var orderJson = JsonSerializer.Serialize(order);
        var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/reply", content);

        app.Logger.LogInformation($"Invoke Successful. Reply received: ${response}");

    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error occurred while invoking App ID. {ex.InnerException}");
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Receive invocation request 
app.MapPost("/reply", (Order order) =>
{
    app.Logger.LogInformation("Request received : " + order);
    return Results.Ok(order);
});

#endregion

#region KV API

//Retrieve state
app.MapGet("/getkv", async ([FromBody] Order order) =>
{
    // Store state in managed diagrid state store 
    try
    {
        var kv = await client.GetStateAsync<Order>("kvstore", order.OrderId.ToString());
        app.Logger.LogInformation($"Get KV Successful. Order retrieved: {order}");
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error occurred while retrieving order: {order.OrderId}. {ex.InnerException}");
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Save state 
app.MapPost("/savekv", async (Order order) =>
{
    // Store state in managed diagrid state store 
    try
    {
        await client.SaveStateAsync("kvstore", order.OrderId.ToString(), order);
        app.Logger.LogInformation($"Save KV Successful. Order saved: {order.OrderId}");
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error occurred while saving order: {order.OrderId}. {ex.InnerException}");
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Delete state 
app.MapDelete("/deletekv", async ([FromBody] Order order) =>
{
    // Store state in managed diagrid state store 
    try
    {
        await client.DeleteStateAsync("kvstore", order.OrderId.ToString());
        app.Logger.LogInformation($"Delete KV Successful. Order deleted: {order.OrderId}");
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error occurred while deleting order: {order.OrderId}. {ex.InnerException}");
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

#endregion

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);