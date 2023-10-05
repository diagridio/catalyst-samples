# Quickstart: PubSub in Java

This guide demonstrates how to set up and run a Java pubsub application using Diagrid CRA. The project consists of `checkout` (publisher) and `order-processor` (consumer) services, along with a managed pubsub broker.
 

# Table of Contents

1. [Prerequisites](#prerequisites)
2. [Install Diagrid CLI](#install-diagrid-cli)
3. [Login and Setup Project](#login-and-setup-project)
4. [Clone and Build the Java Applications](#clone-and-build-the-java-applications)
5. [Setup Remote AppIds in CRA](#setup-remote-appids-in-cra)
6. [Local Application Setup](#local-application-setup)
7. [Run Applications](#run-applications)
8. [Subscribe the Consumer Application to a Topic](#subscribe-the-consumer-application-to-a-topic)
9. [Monitor CRA API Logs](#monitor-cra-api-logs)
10. [Interact with CRA APIs](#interact-with-cra-apis)
11. [Stop Local Applications](#stop-local-applications)
12. [Clean Up Cloud Resources](#clean-up-cloud-resources)
 

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

## Clone and Build the Java Applications
Clone the quickstart repository and navigate:

```bash
git clone git@github.com:diagridio/cra-quickstarts.git
cd cra-quickstarts/pub_sub/java
```

Build the Java applications:

```bash
cd checkout 
mvn clean install

cd ../order-processor
mvn clean install
```
You've now prepared two Java applications for local execution. They're designed to integrate with Diagrid CRA using the Dapr Java SDK.

## Setup Remote AppIds in CRA
Each local application needs a remote representation in CRA, termed as an `appId`.

Create two remote appIds:

```bash
diagrid appid create checkout
diagrid appid create order-processor
```
An appId in CRA serves as the remote identity of a local application. It ensures that the application can seamlessly interact with CRA services, receive messages, and handle requests. Essentially, the appId functions as the single point of contact for all interactions between the application and CRA, providing a unique identity for the application.

## Local Application Setup
To ensure local applications communicate with their `appId`s, specific environment variables are needed. The Diagrid CLI can generate a configuration file with these details.

While it's possible to manually fetch and set these environment variables by accessing the CRA web console, the Diagrid CLI offers a more efficient approach. The CLI provides automation tools that can generate a configuration file containing all the necessary connection details for each application, ensuring they can interact with their respective `appId`s without manual intervention.

Execute the following commands to generate this configuration:

```bash
cd ..
diagrid dev scaffold
```

This command produces a `dev-quickstarts.yaml` file. Edit this file to specify the Java binaries' location, the commands to run the Java classes, and any port numbers for incoming connections.

## Run Applications
Start the applications:

```bash
diagrid dev start
```

This command does the following:

- **Application Initialization**: For each `appId` in the `dev-quickstarts.yaml`, the command uses the `workDir` to locate and start the application with the given command. Both Java applications run on your local machine.

- **Network Tunnel Creation**: If a specific port number is set (e.g., port `8080` for `order-processor`), the Diagrid CLI establishes a network tunnel. This ensures traffic from CRA is routed to the correct port on your localhost.

- **Log Streaming**: The command also streams application logs directly to your terminal, providing real-time feedback.

This command offers a seamless bridge between local development and the Diagrid CRA platform.
 
## Subscribe the Consumer Application to a Topic

With our setup, we have two applications running locally: the `checkout` service and the `order-processor`. While the `checkout` service is actively producing messages to the `orders` topic of the pubsub broker, the `order-processor` isn't yet set up to consume these messages. To bridge this gap, we need to create a subscription for the `order-processor`.

```bash
diagrid subscription create pubsub-order-processor --connection pubsub --topic orders --route /orders --scopes order-processor
```

After a brief period, the `order-processor` will start receiving messages from the `orders` topic.

## Monitor CRA API Logs
Observe the CRA API logs in a new terminal:

```bash
diagrid appid logs checkout -t 10 -f
diagrid appid logs order-processor -t 10 -f
```

These commands stream logs for the `checkout` and `order-processor` appIds, providing insights into interactions with Diagrid CRA.

## Interact with CRA APIs

For direct interaction with CRA APIs, you can use the following commands:

```bash
diagrid test publish orders --connection pubsub --data '{"orderId":999}' --app-id checkout
diagrid test kv get 999 --connection kvstore --app-id order-processor -o json 
```

The first command simulates the `checkout` service by publishing a new order. The second retrieves this order from the Key/Value store.

## Stop Local Applications

After testing and validating your applications, it's a good practice to clean up and release resources.

To stop the local applications, use:

```bash
diagrid dev stop
```

Executing this command performs the following actions: 

- **Connection Termination**: Any active network tunnels or incoming connections established by the Diagrid CLI for your applications are terminated. This ensures there are no lingering connections that might interfere with subsequent operations.

- **Local Application Termination**: It gracefully shuts down the local applications that were started using the Diagrid CLI.

- **appID Configuration Reversion**: The `appId` configuration, which was temporarily pointing to the CLI tunnel for local development, will revert to its original value. This means that any external interactions with the `appId` will no longer be directed to your local machine.
 

Alternatively, use `CTRL + C` in the terminal. This stops applications but retains the network tunnel and `appId` configuration, useful if you plan to restart the applications soon.

## Clean Up Cloud Resources
Clean up the cloud resources:

- Delete specific appIds:

```bash
diagrid appid delete checkout 
diagrid appid delete order-processor
```

By executing these commands, the `checkout` and `order-processor` appIds will be removed from the Diagrid CRA platform. However, other resources associated with the project, like the managed pubsub broker or key/value store, will remain intact.

- **Delete the Entire Project**: If you wish to remove all resources associated with the `quickstarts` project, including appIds, managed services, and configurations, you can delete the entire project:

```bash
diagrid project delete quickstarts
```

Always double-check before executing deletion commands as they're irreversible.
 