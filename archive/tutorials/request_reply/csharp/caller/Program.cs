using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
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
app.MapPost("/consume", (Order order) =>
{
    app.Logger.LogInformation($"Message received: {order.OrderId}");
    return Results.Ok();
});

#endregion

#region Request/Reply API 

// var daprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");

// Invoke another service
app.MapPost("/sendrequest", async (Order order) =>
{
    // Publish order to Diagrid pubsub, topic: orders 
    try
    {
        // Create invoke client for the "target" App ID
        var httpClient = DaprClient.CreateInvokeHttpClient("target", null);

        var orderJson = JsonSerializer.Serialize(order);

        var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/receiverequest", content);

        if (!response.IsSuccessStatusCode)
            app.Logger.LogError("Invocation unsuccessful with Status Code: {0}", response.StatusCode);
        else
            app.Logger.LogInformation("Invocation successful with Status Code: {0}", response.StatusCode);
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error occurred while invoking App ID. {ex.InnerException}");
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Receive invocation request 
app.MapPost("/receiverequest", (Order order) =>
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