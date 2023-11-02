using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var client = new DaprClientBuilder().Build();

var DaprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
var PubSubName = Environment.GetEnvironmentVariable("PUBSUB_NAME") ?? "pubsub";
var KVStoreName = Environment.GetEnvironmentVariable("KVSTORE_NAME") ?? "kvstore";

// Dapr will send serialized event object vs. being raw CloudEvent
app.UseCloudEvents();

#region Publish Subscribe API 

// Publish messages 
app.MapPost("/pubsub/orders", async (Order order) =>
{
    // Publish order to Diagrid pubsub, topic: orders 
    try
    {
        await client.PublishEventAsync(PubSubName, "orders", order);
        app.Logger.LogInformation("Publish Successful. Order published: {orderId}", order.OrderId);

    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while publishing order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

// Subscribe to messages 
app.MapPost("/pubsub/neworders", (Order order) =>
{
    app.Logger.LogInformation("Order received: {orderId}", order.OrderId);
    return Results.Ok();
});

#endregion

#region Request/Reply API 

// Invoke another service
app.MapPost("/invoke/orders", async (Order order) =>
{
    try
    {
        // Create invoke client for the "target" App ID
        var httpClient = DaprClient.CreateInvokeHttpClient("target");
        httpClient.DefaultRequestHeaders.Add("dapr-api-token", DaprApiToken);
        var orderJson = JsonSerializer.Serialize(order);
        var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/invoke/neworders", content);

        if (response.IsSuccessStatusCode) {
            app.Logger.LogInformation("Invocation successful with status code {statusCode}", response.StatusCode);
        } else {
            app.Logger.LogError("Invocation unsuccessful with status code {statusCode}", response.StatusCode);
            return Results.StatusCode((int)response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while invoking App ID: {exception}", ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

app.MapPost("/invoke/neworders", (Order order) =>
{
    app.Logger.LogInformation("Request received : {order}", order);
    return Results.Ok(order);
});

#endregion

#region KV API

// Save state 
app.MapPost("/kv/orders", async (Order order) =>
{
    // Store state in managed diagrid state store 
    try
    {
        await client.SaveStateAsync(KVStoreName, order.OrderId.ToString(), order);
        app.Logger.LogInformation("Save KV Successful. Order saved: {order}", order.OrderId);
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while saving order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(order);
});

//Retrieve state
app.MapGet("/kv/orders/{orderId}", async ([FromRoute] int orderId) =>
{
    // Store state in managed diagrid state store 
    try
    {
        var kv = await client.GetStateAsync<Order>(KVStoreName, orderId.ToString());
        if (kv != null) {
            app.Logger.LogInformation("Get KV Successful. Order retrieved: {order}", orderId.ToString());
            return Results.StatusCode(200);
        } else {
            app.Logger.LogInformation("Key {key} does not exist", orderId.ToString());
            return Results.StatusCode(204);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while retrieving order: {order}. Exception: {exception}", orderId.ToString(), ex.InnerException);
        return Results.StatusCode(500);
    }
});

// Delete state 
app.MapDelete("/kv/orders/{orderId}", async ([FromRoute] int orderId) =>
{
    // Store state in managed diagrid state store 
    try
    {
        await client.DeleteStateAsync(KVStoreName, orderId.ToString());
        app.Logger.LogInformation("Delete KV Successful. Order deleted: {order}", orderId.ToString());
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while deleting order: {order}. Exception: {exception}", orderId.ToString(), ex.InnerException);
        return Results.StatusCode(500);
    }

    return Results.Ok(orderId.ToString());
});

#endregion

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);