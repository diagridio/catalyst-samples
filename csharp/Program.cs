using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var client = new DaprClientBuilder().Build();

var DaprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

#region Publish Subscribe API 

// Publish messages 
app.MapPost("/publish", async (Order order) =>
{
    // Publish order to Diagrid pubsub, topic: orders 
    try
    {
        await client.PublishEventAsync("pubsub", "orders", order);
        app.Logger.LogInformation("Publish Successful. Order published: {order}", order);

    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while publishing order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Subscribe to messages 
app.MapPost("/consume", (Order order) =>
{
    app.Logger.LogInformation("Message received: {order}", order);
    return Results.Ok();
});

#endregion

#region Request/Reply API 

// Invoke another service
app.MapPost("/sendrequest", async (Order order) =>
{
    try
    {
        // Create invoke client for the "target" App ID
        var httpClient = DaprClient.CreateInvokeHttpClient("target");
        httpClient.DefaultRequestHeaders.Add("dapr-api-token", DaprApiToken);
        var orderJson = JsonSerializer.Serialize(order);
        var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/receiverequest", content);

        if (response.IsSuccessStatusCode)
            app.Logger.LogInformation("Invocation successful with status code {statusCode}", response.StatusCode);
        else
            app.Logger.LogError("Invocation unsuccessful with status code {statusCode}", response.StatusCode);


    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while invoking App ID: {exception}", ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

app.MapPost("/receiverequest", (Order order) =>
{
    app.Logger.LogInformation("Request received : {order}", order);
    return Results.Ok(order);
});

#endregion

#region KV API

//Retrieve state
app.MapPost("/getkv", async ([FromBody] Order order) =>
{
    // Store state in managed diagrid state store 
    try
    {
        var kv = await client.GetStateAsync<Order>("kvstore", order.OrderId.ToString());
        if (kv != null)
            app.Logger.LogInformation("Get KV Successful. Order retrieved: {order}", order.OrderId);
        else
            app.Logger.LogInformation("Key {key} does not exist", order.OrderId);
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
        app.Logger.LogInformation("Save KV Successful. Order saved: {order}", order.OrderId);
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while saving order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Delete state 
app.MapPost("/deletekv", async ([FromBody] Order order) =>
{
    // Store state in managed diagrid state store 
    try
    {
        await client.DeleteStateAsync("kvstore", order.OrderId.ToString());
        app.Logger.LogInformation("Delete KV Successful. Order deleted: {order}", order.OrderId);
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while deleting order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

#endregion

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);