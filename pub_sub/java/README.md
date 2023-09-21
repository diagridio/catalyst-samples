# Quickstart: PubSub in Java

This guide demonstrates how to set up and run a Java pubsub application using Diagrid CRA. The project consists of `checkout` (publisher) and `order-processor` (consumer) services, along with a managed pubsub broker.

## Table of Contents

- [Quickstart: PubSub in Java](#quickstart-pubsub-in-java)
- [Prerequisites](#prerequisites)
- [Install Diagrid CLI](#install-diagrid-cli)
- [Login and Setup Project](#login-and-setup-project)
- [Clone and Build the Java Applications](#clone-and-build-the-java-applications)
- [Setup Remote AppIds in CRA](#setup-remote-appids-in-cra)
- [Local Application Setup](#local-application-setup)
- [Run Applications](#run-applications)
- [Subscribe the Consumer Application to a Topic](#subscribe-the-consumer-application-to-a-topic)
- [Monitor CRA API Logs](#monitor-cra-api-logs)
- [Interact with CRA APIs](#interact-with-cra-apis)
- [Cleanup](#cleanup)
- [Clean Up Cloud Resources](#clean-up-cloud-resources)

---

You can place this Table of Contents at the beginning of your README or documentation. Clicking on any of the links will take the reader directly to the corresponding section.

## Prerequisites
- Diagrid CRA account
- Java 11 or newer
- Apache Maven
- Git client
- Curl

---

## Install Diagrid CLI
To begin, install the Diagrid CLI:

```bash
curl -o- https://downloads.stg.diagrid.io/cli/install-cra.sh | bash
```

## Login and Setup Project
To begin, you'll need to login to your Diagrid account and set up a new project:

```bash
diagrid login --api https://api.diagrid.io
diagrid project create quickstarts --deploy-managed-pubsub --deploy-managed-kv
diagrid project use quickstarts
```

In Diagrid CRA, a project serves as a mechanism to group resources, facilitating easier management and ensuring isolation. By executing the above commands, you'll authenticate with your Diagrid account, create a new project named quickstarts provisioned with default managed services (specifically a pubsub broker and a key-value store), and configure the CLI to target this newly created project for all subsequent commands. This ensures that any resources you create or manage will be associated with this project.

## Clone and Build the Java Applications
Start by cloning the quickstart repository and navigating to the pubsub/java folder:

```bash
git clone git@github.com:diagridio/cra-quickstarts.git
cd pub_sub/java
```

Next, build each Java application using Apache Maven:

```bash
cd checkout 
mvn clean install

cd ../order-processor
mvn clean install
```
At this point, you have successfully cloned and built two Java applications. They are now ready to be run locally. These applications are designed to interact with the Diagrid CRA platform using Dapr Java SDK, and the build process ensures that all necessary dependencies and configurations are in place for smooth execution and integration with CRA.


## Setup Remote AppIds in CRA
When working with the Diagrid CRA platform, it's essential to have a remote representation for each application. This representation is achieved using an appId.

To represent each local Java application in CRA, create two remote appIds:

```bash
diagrid appid create checkout
diagrid appid create order-processor
```
An appId in CRA serves as the remote identity of an application. It functions as the single point of contact for all interactions between the application and CRA.

An appId in CRA serves as the remote identity of a local application. It ensures that the application can seamlessly interact with CRA services, receive messages, and handle requests. Essentially, the appId functions as the single point of contact for all interactions between the application and CRA, providing a unique identity for the application.
Certainly! Here's the extended section:

---

## Local Application Setup

When developing applications that interact with the Diagrid CRA platform, it's crucial to ensure that the local applications can  communicate with their remote representations, the `appId`s. This communication is facilitated by specific environment variables that provide the necessary connection details, such as the endpoint URLs and unique API tokens. These variables are recognized and utilized by the Dapr SDKs embedded within the applications.

While it's possible to manually fetch and set these environment variables by accessing the CRA web console, the Diagrid CLI offers a more efficient approach. The CLI provides automation tools that can generate a configuration file containing all the necessary connection details for each application, ensuring they can interact with their respective `appId`s without manual intervention.

Execute the following commands to generate this configuration:

```bash
cd ..
diagrid dev scaffold
```

Upon execution, the diagrid dev scaffold command generates a file named dev-{project-name}.yaml in the current directory. This file contains the basic configuration needed for your local applications to communicate with their respective appIds in CRA.

However, the generated configuration is not complete out of the box. It requires some additional adjustments to ensure the local applications run correctly. Specifically, in our case, we need to specify the location of the Java binaries to run, the commands to execute the Java classes, and any port numbers for incoming connections.

By having this dev-quickstarts.yaml file, you're provided with a structured template that can be easily customized to fit the requirements of your local applications, ensuring seamless integration with the Diagrid CRA platform.


Certainly! Here's the revised section:

---

## Configure Local Application Setup

Edit the generated `dev-quickstarts.yaml` file to set up the configurations for each application:

### Configuration for `appId` **checkout**:

- Update `workDir` to:
  ```yaml
  workDir: "checkout"
  ```
- Update the command to:
  ```yaml
  command: ["java -jar target/CheckoutService-0.0.1-SNAPSHOT.jar --server.port=8282"]
  ```

For the `checkout` application, we've set the working directory to its respective folder and specified the command to run its Java binary. We've also designated port `8282` for this application to avoid any port conflicts, especially with the `order-processor` application that uses port `8080`.

### Configuration for `appId` **order-processor**:

- Update `workDir` to:
  ```yaml
  workDir: "order-processor"
  ```
- Update the command to:
  ```yaml
  command: ["java -jar target/OrderProcessingService-0.0.1-SNAPSHOT.jar"]
  ```
- Set the port to:
  ```yaml
  port: 8080
  ```

For the `order-processor` application, we've again set the working directory and provided the command to run its Java binary. Additionally, we've specified port `8080` for incoming connections. Unlike the `checkout` application, which primarily sends outgoing requests, the `order-processor` application is designed to handle incoming connections. These connections originate from CRA and wil be directed to the local machine on port `8080`.

 
---

## Run Applications

To start the local applications, execute the following command:

```bash
diagrid dev start
```

This command does the following:

- **Application Initialization**: For each `appId` in the `dev-quickstarts.yaml`, the command uses the `workDir` to locate and start the application with the given command. Both Java applications run on your local machine.

- **Network Tunnel Creation**: If a specific port number is set (e.g., port `8080` for `order-processor`), the Diagrid CLI establishes a network tunnel. This ensures traffic from CRA is routed to the correct port on your localhost.

- **Log Streaming**: The command also streams application logs directly to your terminal, providing real-time feedback.

This command offers a seamless bridge between local development and the Diagrid CRA platform.

---
 
## Subscribe the Consumer Application to a Topic

With our setup, we have two applications running locally: the `checkout` service and the `order-processor`. While the `checkout` service is actively producing messages to the `orders` topic of the pubsub broker, the `order-processor` isn't yet set up to consume these messages. To bridge this gap, we need to create a subscription for the `order-processor`.

By subscribing the `order-processor` appId to the `orders` topic, we ensure that it can receive and process messages sent to this topic. Execute the following command to create this subscription:

```bash
diagrid subscription create pubsub-order-processor --connection pubsub --topic orders --route /orders --scopes order-processor
```

After running the command, there's a brief propagation period. It might take a few seconds for the subscription changes to be recognized and activated. Once this happens, the `order-processor` application will start receiving messages from the `orders` topic. You can monitor this activity in real-time by observing the logs. Any messages sent to the `orders` topic by the `checkout` service will now be routed to and processed by the `order-processor` on your local machine.


## Monitor CRA API Logs

To observe the CRA API logs, open a separate terminal:

```bash
diagrid appid logs checkout -t 10 -f
diagrid appid logs order-processor -t 10 -f
```

These commands stream the logs for the `checkout` and `order-processor` appIds:

- `-t 10`: Fetches the last 10 log entries.
- `-f`: Continuously streams logs in real-time.

Monitoring these logs provides insights into your applications' interactions with the Diagrid CRA platform and helps identify any issues.

 
## Interact with CRA APIs

For direct interaction with CRA APIs, you can use the following commands:

```bash
diagrid test publish post --connection pubsub --data '{"orderId":100}' --app-id checkout
diagrid test kv get 1 --connection kvstore --app-id order-processor -o json 
```

- The first command simulates the `checkout` service by publishing a new order with `orderId` of `100` to the pubsub broker.
- The second command retrieves the newly published order from the Key/Value store using its `orderId`.

These commands allow you to test and validate the data flow within your applications.

---

## Cleanup

After testing and validating your applications, it's a good practice to clean up and release resources.

To stop the local applications, use:

```bash
diagrid dev stop
```

Executing this command performs the following actions:
- 
- **Connection Termination**: Any active network tunnels or incoming connections established by the Diagrid CLI for your applications are terminated. This ensures there are no lingering connections that might interfere with subsequent operations.

- **Local Application Termination**: It gracefully shuts down the local applications that were started using the Diagrid CLI.

- **appID Configuration Reversion**: The `appId` configuration, which was temporarily pointing to the CLI tunnel for local development, will revert to its original value. This means that any external interactions with the `appId` will no longer be directed to your local machine.
 

If you prefer, you can also manually stop the applications by pressing `CTRL + C` in the terminal where the `dev` command is running. This action will halt the applications but will preserve the network tunnel and `appId` configuration. This is useful if you anticipate restarting the local applications shortly after. By preserving the tunnel and configuration, you can quickly resume operations without needing to re-establish the connection or reconfigure the `appId`.

---

## Clean Up Cloud Resources

After completing your development and testing, it's a good practice to clean up the cloud resources you've provisioned to avoid incurring unnecessary costs and to maintain a tidy environment.

- **Delete Individual appIds**: If you only want to remove specific appIds without affecting other resources, you can delete them individually:

```bash
diagrid appid delete checkout 
diagrid appid delete order-processor
```

By executing these commands, the `checkout` and `order-processor` appIds will be removed from the Diagrid CRA platform. However, other resources associated with the project, like the managed pubsub broker or key/value store, will remain intact.

- **Delete the Entire Project**: If you wish to remove all resources associated with the `quickstarts` project, including appIds, managed services, and configurations, you can delete the entire project:

```bash
diagrid project delete quickstarts
```

This command ensures a comprehensive cleanup, leaving no remnants of the `quickstarts` project on the Diagrid CRA platform.

Remember to always double-check before executing deletion commands, as the removal is permanent and cannot be undone.

---