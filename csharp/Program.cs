using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Dapr.Workflow;
using BasicWorkflowSamples;

var builder = WebApplication.CreateBuilder(args);

// Add workflow 
builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterWorkflow<HelloWorldWorkflow>();
    options.RegisterActivity<CreateGreetingActivity>();
});

var app = builder.Build();

// Catalyst: Ensure environment variable DAPR_GRPC_ENDPOINT and DAPR_API_TOKEN is set before this point
var client = new DaprClientBuilder().Build();
var workflowClient = app.Services.GetRequiredService<DaprWorkflowClient>();

var DaprApiToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN") ?? "";
var PubSubName = Environment.GetEnvironmentVariable("PUBSUB_NAME") ?? "pubsub";
var KVStoreName = Environment.GetEnvironmentVariable("KVSTORE_NAME") ?? "kvstore";
var InvokeTargetAppID = Environment.GetEnvironmentVariable("INVOKE_APPID") ?? "target";
var WorkflowStateStore = Environment.GetEnvironmentVariable("WORKFLOW_STORE") ?? "kvstore";

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

#region Request/Reply API 

// Invoke another service
app.MapPost("/invoke/orders", async (Order order) =>
{
    try
    {
        // Create invoke client for the "target" App ID
        var httpClient = DaprClient.CreateInvokeHttpClient(InvokeTargetAppID);
        httpClient.DefaultRequestHeaders.Add("dapr-api-token", DaprApiToken);
        var orderJson = JsonSerializer.Serialize(order);
        var content = new StringContent(orderJson, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/invoke/neworders", content);

        if (response.IsSuccessStatusCode)
        {
            app.Logger.LogInformation("Invocation successful with status code {statusCode}", response.StatusCode);
            return Results.StatusCode(200);
        }
        else
        {
            app.Logger.LogError("Invocation unsuccessful with status code {statusCode}", response.StatusCode);
            return Results.StatusCode(500);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while invoking App ID: {exception}", ex.InnerException);
        return Results.StatusCode(500);
    }
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
        return Results.StatusCode(200);
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while saving order: {orderId}. Exception: {exception}", order.OrderId, ex.InnerException);
        return Results.StatusCode(500);
    }
});

//Retrieve state
app.MapGet("/kv/orders/{orderId}", async ([FromRoute] int orderId) =>
{
    // Store state in managed diagrid state store 
    try
    {
        var kv = await client.GetStateAsync<Order>(KVStoreName, orderId.ToString());
        if (kv != null)
        {
            app.Logger.LogInformation("Get KV Successful. Order retrieved: {order}", orderId.ToString());
            return Results.Ok(kv);
        }
        else
        {
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
        return Results.StatusCode(200);
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while deleting order: {order}. Exception: {exception}", orderId.ToString(), ex.InnerException);
        return Results.StatusCode(500);
    }
});

#endregion

#region Workflow API

// Start new workflow
app.MapPost("/workflow/start", async (Greeting greeting) =>
{
    // Store state in managed diagrid state store 
    var guid = Guid.NewGuid();
    try
    {
        await workflowClient.ScheduleNewWorkflowAsync(
            name: nameof(HelloWorldWorkflow),
            input: greeting.Input,
            instanceId: guid.ToString());
        
        app.Logger.LogInformation("Started a new HelloWorld Workflow with id {guid} and input {input}", guid, greeting.Input);
        return Results.Ok(guid);
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while starting workflow: {guid}. Exception: {exception}", guid, ex.InnerException);
        return Results.StatusCode(500);
    }
});

// Get workflow status
app.MapGet("/workflow/status/{id}", async ([FromRoute] string id) =>
{
    try
    {
        WorkflowState state = await workflowClient.GetWorkflowStateAsync(
            instanceId: id);

        app.Logger.LogInformation("STATE: {state}", state.ToString());

         if (state != null)
        {
            app.Logger.LogInformation("Get Workflow status successful. Workflow Runtime Status is: {status} ", state.RuntimeStatus);
            return Results.Ok(state);
        }
        else
        {
            app.Logger.LogInformation("Workflow with id {id} does not exist", id);
            return Results.StatusCode(204);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while getting the status of the workflow: {id}. Exception: {exception}", id, ex.InnerException);
        return Results.StatusCode(500);
    }
});

// Get completed workflow output
app.MapGet("/workflow/output/{id}", async ([FromRoute] string id) =>
{
    try
    {
        WorkflowState state = await workflowClient.GetWorkflowStateAsync(
            instanceId: id);

        app.Logger.LogInformation("STATE: {state}", state.ToString());

        var output = state.ReadOutputAs<String>();

         if (state != null)
        {
            app.Logger.LogInformation("Get Workflow output successful. Workflow Output is: {output} ", output);
            return Results.Ok(output);
        }
        else
        {
            app.Logger.LogInformation("Workflow with id {id} does not exist", id);
            return Results.StatusCode(204);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError("Error occurred while getting the output of the workflow: {id}. Exception: {exception}", id, ex.InnerException);
        return Results.StatusCode(500);
    }
});

#endregion

app.Run();

public record Order([property: JsonPropertyName("orderId")] int OrderId);
public record Greeting([property: JsonPropertyName("input")] string Input);