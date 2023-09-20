# Quickstart: PubSub in Java

This guide demonstrates how to set up and run a Java pubsub application using Diagrid CRA. The project consists of `checkout` (publisher) and `order-processor` (consumer) services, along with a managed pubsub broker.

## Prerequisites
- Diagrid CRA account
- Java 11 or newer
- Apache Maven
- Git client
- Curl

## Table of Contents
1. [Install Diagrid CLI](#install-diagrid-cli)
2. [Login and Setup Project](#login-and-setup-project)
3. [Setup Remote Apps in CRA](#setup-remote-apps-in-cra)
4. [Clone and Build Sample Application](#clone-and-build-sample-application)
5. [Generate and Edit Local Configuration](#generate-and-edit-local-configuration)
6. [Run Applications](#run-applications)
7. [Observe CRA API Logs](#observe-cra-api-logs)
8. [Interact with CRA APIs](#interact-with-cra-apis)
9. [Cleanup](#cleanup)

---

## Install Diagrid CLI
To begin, install the Diagrid CLI:

```bash
curl -o- https://downloads.stg.diagrid.io/cli/install-cra.sh | bash
```

## Login and Setup Project
Login to your Diagrid account and set up a new project:

```bash
diagrid login --api https://api.stg.diagrid.io
diagrid project create quickstarts --deploy-managed-pubsub --deploy-managed-kv
diagrid project use quickstarts
```

## Setup Remote Apps in CRA
Create remote representations of the local services.

```bash
diagrid appid create checkout
diagrid appid create order-processor
```

Subscribe the `order-processor` app to the `orders` topic:

```bash
diagrid subscription create pubsub-order-processor --connection pubsub --topic orders --route /orders --scopes order-processor
```

## Clone and Build Sample Application
Clone the quickstart repository and navigate to this sample application:

```bash
git clone git@github.com:diagridio/cra-quickstarts.git
cd pub_sub/java
```

Build both Java applications using Maven:

```bash
cd checkout 
mvn clean install

cd ../order-processor
mvn clean install
```

## Generate and Edit Local Configuration
Generate a local configuration file:

```bash
cd ..
diagrid dev scaffold
```

Edit the generated file to configure the applications:

For `appId` **checkout**:
- Update `workDir` to:
  ```yaml
  workDir: "checkout"
  ```
- Update the command to:
  ```yaml
  command: ["java -jar target/CheckoutService-0.0.1-SNAPSHOT.jar --server.port=8282"]
  ```
  Note: We run this app on port `8282` to avoid a clash with port `8080` used by `order-processor`.

For `appId` **order-processor**:
- Update `workDir` to:
  ```yaml
  workDir: "order-processor"
  ```
- Update the command to:
  ```yaml
  command: ["java -jar target/OrderProcessingService-0.0.1-SNAPSHOT.jar"]
  ```
- Update the port to:
  ```yaml
  port: 8080
  ```
  This configuration instructs the Diagrid CLI to direct any traffic destined for the app to port `8080` on localhost. The initial creation of this network tunnel takes a few seconds, but subsequent runs are instant.

---

## Run Applications
Start both applications and view their logs:

```bash
diagrid dev start
```

In a separate terminal, observe the CRA API logs:

```bash
diagrid appid logs checkout -t 10 -f
diagrid appid logs order-processor -t 10 -f
```

## Interact with CRA APIs
Optionally, you can interact directly with CRA APIs and verify the orders are saved in the Key/Value store.

```bash
diagrid test kv get 1 --connection kvstore --app-id order-processor -o json 

diagrid test publish post --connection pubsub --data '{"orderId":100}' --app-id checkout
```



## Cleanup
Stop the local applications:

```bash
diagrid dev stop
```

Or use `CTRL + C` in the terminal running the `dev` command.

To clean up cloud resources:

- Delete both appIds:

```bash
diagrid appid delete checkout 
diagrid appid delete order-processor
```

- Or delete the entire project:

```bash
diagrid project delete quickstarts
```
