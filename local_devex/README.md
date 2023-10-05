# Quickstart: Diagrid CLI Local Experience Tutorial

This guide showcases how to streamline the local development experience using Diagrid CLI commands. In this tutorial, we focus on two Java applications: the publisher application and the consumer application. The primary objective is to illustrate an efficient inner development flow, emphasizing the capabilities and utilities of the Diagrid CLI.
 
## Prerequisites
Before diving in, ensure you have the following:

- Diagrid CRA account
- Java 11 or newer
- Apache Maven
- Git client
- Curl

## Install Diagrid CLI
To begin, install the Diagrid CLI:

```bash
curl -o- https://downloads.stg.diagrid.io/cli/install-cra.sh | bash
```

## Login and Setup Project

Before diving into the setup, check if you already have a project created in Diagrid CRA. If there's no existing project, you'll need to create a new one named "quickstarts". If you already have a project, ensure that it's equipped with the default managed services, specifically a pubsub broker and a kvstore.

Authenticate with your Diagrid account and create a new project:

```bash
diagrid login --api https://api.diagrid.io
diagrid project create quickstarts --deploy-managed-pubsub --deploy-managed-kv
diagrid project use quickstarts
```

In Diagrid CRA, a project serves as a mechanism to group resources, facilitating easier management and ensuring isolation. By executing the above commands, you'll authenticate with your Diagrid account, create a new project named quickstarts provisioned with default managed services (specifically a pubsub broker and a key-value store), and configure the CLI to target this newly created project for all subsequent commands. This ensures that any resources you create or manage will be associated with this project.

## Clone and Build Publisher Application
Clone the quickstart repository and navigate:

```bash
git clone git@github.com:diagridio/cra-quickstarts.git
cd cra-quickstarts/local_devex
```

Build the publisher applications:

```bash
cd publisher 
mvn clean install
```
You've now prepared the publisher applications for local execution. It is designed to integrate with Diagrid CRA using the Dapr Java SDK.

## Setup Remote AppId for Publisher Application
Each local application needs a remote representation in CRA, termed as an `appId`.

Create a remote appId:

```bash
diagrid appid create publisher
```
An appId in CRA serves as the remote identity of an application. It functions as the single point of contact for all interactions between the application and CRA.

## Validate Remote Configurations

Before configuring the local application, it's crucial to ensure that the remote configurations are correctly set up. Specifically, the publisher application should have the necessary permissions to access the default pubsub broker.

To validate this, you can attempt to publish a message to a `test` queue:

```bash
diagrid call publish test-queue --connection pubsub --data '{"orderId":0}' --app-id publisher
```

If everything is set up correctly, you should receive a 204 response code. This confirms that the publisher application (represented by the `appId`) has the required permissions and is ready to publish messages to the default broker.


## Run Publisher Application
To ensure local applications communicate with their `appId`s, specific environment variables are needed. The Diagrid CLI can fetch these details, pass to your applicaiton and run in a single command.

Execute the following commands to run the application and connect to its appId

```bash
diagrid dev start --app-id publisher "java -jar target/PublisherApplication-0.0.1-SNAPSHOT.jar --server.port=8282"
```
diagrid dev start is a powerful command. It can run a single applicaiton as seen here, or run multiple applicaiton as we will see later. It can also open incoming connections from CRA to local ports for any ingress traffic. Publisher application acts only as message publisher and it doesn't require any incoming connections.

The command above does the following:

- **Application Initialization**: The command uses the `workDir` to locate and start the application with the given command and run on your local machine.

- **Network Tunnel Creation**: If a specific port number is set (e.g., `--app-por 8080`), the Diagrid CLI establishes a network tunnel. This ensures traffic from CRA is routed to the correct port on your localhost.

- **Log Streaming**: The command also streams application logs directly to your terminal, providing real-time feedback.


## Monitor CRA API Logs
Observe the CRA API logs in a new terminal:

```bash
diagrid appid logs publisher -t 10 -f
```

These commands stream API logs for the `publisher` appId, providing insights into interactions with Diagrid CRA.


Build the Consumer application:

```bash

cd ../consumer
mvn clean install
```

## Setup Consumer AppId
Create consumer remote appIds:

```bash
diagrid appid create consumer
```
 
## Subscribe the Consumer Application to `orders` Topic

With our setup, we have two applications running locally: the `publisher` service and the `consumer`. While the `publisher` service is actively producing messages to the `orders` topic of the pubsub broker, the `consumer` isn't yet set up to consume these messages. To bridge this gap, we need to create a subscription.

```bash
diagrid subscription create pubsub-consumer --connection pubsub --topic orders --route /orders --scopes consumer
```


## Validate the Subscription Configurations


Before diving into the consumer application's setup, it's essential to verify that its remote configurations are in order. The quickest way to do this is by listening for events directed to the consumer application's subscription directly from diagrid CLI.


```bash
diagrid listen --app-id consumer --subscription pubsub-consumer
```

After a short wait, if the configurations are correct, the consumer should begin receiving messages from the `orders` topic. Once you've confirmed the setup's accuracy, you can stop the CLI listener and proceed to launch the actual consumer application.


## Launching the Consumer Application

To initiate the consumer application and establish a connection with its corresponding appId, follow the steps below:

```bash
diagrid dev start --app-id consumer --app-port 8080 "java -jar target/ConsumerApplication-0.0.1-SNAPSHOT.jar"
```

It's important to note that the command includes the --app-port parameter. This parameter sets up an incoming connection from CRA to the local port where the application is actively listening, ensuring seamless communication between the local development environment and the Diagrid platform.
 

## Monitor CRA API Logs
Observe the CRA API logs in a new terminal:

```bash
diagrid appid logs consumer -t 10 -f
```

These commands stream logs for the `consumer` appId providing insights into interactions with Diagrid CRA.

## Interact with CRA APIs

For direct interaction with CRA APIs, you can use the following commands:

```bash
diagrid call publish orders --connection pubsub --data '{"orderId":999}' --app-id publisher
diagrid call kv get 999 --connection kvstore --app-id consumer -o json 
```

The first command simulates the `publisher` application by publishing a new order. The second retrieves this order from the Key/Value store confirming that the consumer application picked the message and stored it into the kvstore.


## Run multiple applications
To run both applications declaratively, you can generate a dev file and run them. This is covered in tutorial X.

```bash

cd ..
diagrid dev scaffold

diagrid dev start

```


## Stop Local Applications

After testing and validating your applications, it's a good practice to clean up and release resources.

To stop the local applications, use:

```bash
diagrid dev stop
diagrid dev stop --app-id consumer
```

## Clean Up Cloud Resources
Clean up the cloud resources:

- Delete specific appIds:

```bash
diagrid appid delete publisher 
diagrid appid delete consumer
diagrid subscription delete pubsub-consumer
```

By executing these commands, the `publisher`, `consumer` appIds, and `pubsub-consumer` subscription will be removed from the Diagrid CRA platform. However, other resources associated with the project, like the managed pubsub broker or key/value store, will remain intact.

- **Delete the Entire Project**: If you wish to remove all resources associated with the `quickstarts` project, including appIds, managed services, and configurations, you can delete the entire project:

```bash
diagrid project delete quickstarts
```

Always double-check before executing deletion commands as they're irreversible.
 