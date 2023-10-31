# Quickstart: Java - Work in Progress
 
## Prerequisites
Before diving in, ensure you have the following:

- Java 11 or newer
- Apache Maven
 
## Clone and Build the Java Applications
Clone the quickstart repository and navigate:

```bash
git clone git@github.com:diagridio/cra-quickstarts.git
cd generic-code/java
```

Build the Java applications:

```bash
mvn clean install
```
You've now prepared two Java applications for local execution. They're designed to integrate with Diagrid CRA using the Dapr Java SDK.

## Setup Remote AppIds in CRA
Each local application needs a remote representation in CRA, termed as an `appId`.

Create two remote appIds:

```bash
diagrid appid create publisher
diagrid appid create subscriber
```
An appId in CRA serves as the remote identity of a local application. It ensures that the application can seamlessly interact with CRA services, receive messages, and handle requests. Essentially, the appId functions as the single point of contact for all interactions between the application and CRA, providing a unique identity for the application.




## Run Publisher Application
To ensure local applications communicate with their `appId`s, specific environment variables are needed. The Diagrid CLI can fetch these details, pass to your applicaiton and run in a single command.

Execute the following commands to run the application and connect to its appId

```bash
diagrid dev start --app-id publisher "java -jar target/Main-0.0.1-SNAPSHOT.jar"
diagrid dev start --app-id subscriber --app-port 9001  "java -jar target/Main-0.0.1-SNAPSHOT.jar --port=9001"
```
diagrid dev start is a powerful command. It can run a single applicaiton as seen here, or run multiple applicaiton as we will see later. It can also open incoming connections from CRA to local ports for any ingress traffic. Publisher application acts only as message publisher and it doesn't require any incoming connections.

The command above does the following:

- **Application Initialization**: The command uses the `workDir` to locate and start the application with the given command and run on your local machine.

- **Network Tunnel Creation**: If a specific port number is set (e.g., `--app-por 8080`), the Diagrid CLI establishes a network tunnel. This ensures traffic from CRA is routed to the correct port on your localhost.

- **Log Streaming**: The command also streams application logs directly to your terminal, providing real-time feedback.

## Subscribe the Consumer Application to a Topic

With our setup, we have two applications running locally: the `checkout` service and the `order-processor`. While the `checkout` service is actively producing messages to the `orders` topic of the pubsub broker, the `order-processor` isn't yet set up to consume these messages. To bridge this gap, we need to create a subscription for the `order-processor`.

```bash
diagrid subscription create order-consumer --connection pubsub --topic orders --route /subscribe --scopes subscriber
```

After a brief period, the `order-processor` will start receiving messages from the `orders` topic.
 