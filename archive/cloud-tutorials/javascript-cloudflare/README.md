# Cloud Runtime APIs with Cloudflare

## Tutorial overview

In this tutorial, you will learn how to interact with Diagrid APIs from an event-driven order system running on Cloudflare workers leveraging a combination of Diagrid-hosted and Cloudflare-hosted infrastructure.

The order system consists of two services:

- A Checkout service (JS) that is triggered by HTTP requests and publishes orders to a Pub/Sub broker using the Diagrid Pub/Sub API
- An Order-processor service (JS) that consumes orders from the Pub/Sub broker and persists them in a key-value store via the Diagrid State Management API

![Tutorial Architecture](../../images/js-cloudflare/JS-cloudflare-tutorial.svg)

The tutorial is broken down into the following sections:

- Deployment of Diagrid infrastructure resources including a serverless Pub/Sub broker and Key-value store
- Creation and configuration of Cloud Runtime APIs for Pub/Sub and State Management
- Deployment of applications to Cloudflare Workers
- Validation of end-to-end solution
- Clean up

The application deployment section will dive deeper into the inner workings of the applications and how they leverage the Cloud Runtime APIs.

## Tutorial Setup

| Requirement       | Instructions                                                                                                                                                                                      |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Diagrid Account   | Follow Getting Started section from [here](../../docs/4-support.md). |                          |
| Diagrid CLI       | Follow Getting Started section from [here](../../docs/4-support.md). |
| GitHub Account    | Get one for [free](https://github.com/join).            |
| git               | [Install git](https://git-scm.com/downloads)            |
| Cloudflare account     | If you don't have one, [create an account for free](https://dash.cloudflare.com/sign-up). |
| Wrangler CLI         | Install the [Wrangler CLI](https://developers.cloudflare.com/workers/wrangler/install-and-update/#install-wrangler).                                                           |
| yq utility (Optional) | Install command-line YAML processor [yq](https://github.com/mikefarah/yq#install)                                                                   |
| jq utility (Optional) | Install command-line JSON processor [jq](https://jqlang.github.io/jq/download/)     |

### Cloudflare Setup

1. Sign in to Cloudflare with Wrangler CLI.

    ```bash
    wrangler login
    ```

    This will open a browser window to log in to Cloudflare.

### Cloud Runtime API Setup

1. Sign in to Diagrid Cloud by confirming the browser prompt and setting an organization.

    ```bash
        diagrid login
        diagrid orgs list

        # Confirm your login credentials
        diagrid whoami
    ```

2. Set environment variables which will be used for Cloud Runtime API resources:

    ```bash
    export CRA_PROJECT_NAME="default"
    export PUBSUB_NAME="pubsub"
    export PUBSUB_TOPIC="orders"
    ```

## Deploy CRA Resources

### Create Project

1. If you do not have a default project available in your organization, create a CRA project which will be used to deploy CRA resources related to the tutorial. Otherwise, skip to the following section.

    ```bash
    diagrid project create $CRA_PROJECT_NAME --deploy-managed-pubsub --deploy-managed-kv
    ```

2. Set the default project in the CLI to your newly created project.

    ```bash
    diagrid project use $CRA_PROJECT_NAME
    ```

### Create Application Identities

1. Deploy an Application Identity (App ID) for the Checkout service. An App ID is the CRA representation of your externally hosted service and is used to establish what an application can and can't do within CRA.

    ```bash
    # Ensure you are within the correct project context
    diagrid project use $CRA_PROJECT_NAME

    diagrid appid create checkout-http --wait
    ```

2. Retrieve the API token of the Checkout service. The API token is in the form of `diagrid://v1/<UUID>/<ID>/<PROJECT_NAME>/<APP_NAME>/<UUID>` and will be used to establish connectivity from the checkout application to the Diagrid-hosted App ID.

    ```bash
    export CHECKOUT_API_TOKEN=`diagrid appid get checkout-http -o json | jq -r .status.apiToken`
    ```

3. Repeat the above steps for the Order processor service. Each service should have a unique, dedicated App ID in CRA.

    ```bash
    diagrid appid create order-processor-http --wait

    export ORDERPROCESSOR_API_TOKEN=`diagrid appid get order-processor-http -o json | jq -r .status.apiToken`
    ```

4. Log in to the CRA portal to view the two App Ids that you have just created. Navigate to [https://cra.diagrid.io/app-ids](https://cra.diagrid.io/app-ids) in the browser and sign in with your email address that was invited to Diagrid Cloud.

    ![CRA UI App Ids](../../images/js-cloudflare/JS-cloudflare-ui-appids.png)

### Retrieve Project URL

Retrieve the HTTP endpoint for the project.  This endpoint will be used as the primary point of ingress for connecting to the hosted Diagrid APIs.

```bash
export DAPR_HOST=`diagrid project get $CRA_PROJECT_NAME -o json | jq -r '.status.endpoints.http.url'`
```

## Deploy Cloudflare Resources

The order system is composed of two Javascript applications: a checkout service and an order processor service. Let's dive deeper into the two services and how they leverage the Diagrid APIs. We will deploy both services on Cloudflare Workers and leverage Diagrid Managed PubSub and Cloudflare Workers KV store in the system.

![Cloudflare Workers Solution Overview](../../images/js-cloudflare/JS-cloudflare.svg)

The user flow is defined below:

1. A user initiates the creation of an order via an HTTP request to the Checkout service.
1. The Checkout service utilizes the `Diagrid Pub/Sub API` to publish a message using the `checkout-http` App ID and a Pub/Sub connection called `pubsub` facilitating the message being sent to the Diagrid serverless message broker on the `orders` topic.

   > Note: These values are passed into the application via environment variables. TODO add screenshot here
   <!-- > ![Pub/Sub API](../../images/js-cloudflare/csharp-checkout.png) -->
1. The Order processor service subscribes to messages delivered to the `orders` topic by the Checkout service through a `Diagrid Pub/Sub Subscription`. The orders are delivered to the Order processor service at the `/orders` route.
1. The Order processor uses the `Diagrid State Management API` to store the order as a key-value pair using the `order-processor-http` App ID and an external state connection called `statestorekv` facilitating data persistence to the Cloudflare Workers KV store.
   > Note: These values are passed into the application via environment variables. TODO add screenshot here
   <!-- > ![State API](../../images/js-cloudflare/csharp-checkout.png) -->

The Cloudflare Workers code for both the Checkout and Order processor services has been provided for you but you still need to update the environment variables and deploy the apps. Instructions for development and deployment are found in the following sections.

### Deploy on Cloudflare Workers: Checkout Service

Begin by reviewing the code for the Checkout service and updating the environment variables.

1. Clone this repo and navigate to the Checkout service directory.

    ``` bash
    git clone https://github.com/diagridio/cra-private-beta.git
    cd tutorials/javascript-cloudflare/checkout-http
    ```

2. Inspect the code in the Checkout service's [worker.js](./checkout-http/src/worker.js) file and install the Node dependencies:

    ```bash
    npm install
    ```

3. Update the [vars] section of the Checkout service's [wrangler.toml](./checkout-http/wrangler.toml) file to add the `CHECKOUT_API_TOKEN` and `DAPR_HOST` variables obtained in the previous section.

    ```bash
    echo $DAPR_HOST
    echo $CHECKOUT_API_TOKEN
    ```

    ``` toml
    [vars]
    PUBSUB_NAME = "pubsub"
    PUBSUB_TOPIC = "orders"
    DAPR_HTTP_PORT = 443
    DAPR_HOST = "<YOUR_HOST>"
    CHECKOUT_API_TOKEN = "<YOUR_API_TOKEN>"
    ```

4. Deploy the Checkout service to Cloudflare workers.

    ``` bash
    wrangler deploy
    ```

### Create a Cloudflare Workers KV Namespace

The Order processor service utilizes Cloudflare workers KV store via the Diagrid APIs to persist state.

1. In a new terminal, navigate to the Order processor service directory.

    ```bash
    cd tutorials/javascript-cloudflare/order-processor-http
    ```

2. Create a new Cloudflare Worker KV namespace for the Order processor service.

    ```bash
    export STORAGE_ACCOUNT_NAME="statestorekv"
    wrangler kv:namespace create $STORAGE_ACCOUNT_NAME
    ```

    Expected output:

    ```bash
    🌀 Creating namespace with title "order-processor-http-statestorekv"
    ✨ Success!
    Add the following to your configuration file in your kv_namespaces array:
    { binding = "statestorekv", id = "<YOUR_KV_ID>" }
    ```

3. Copy the KV id from the output and paste it into the [connections/workers-kv.yaml](./connections/workers-kv.yaml) file in the `kvNamespaceID` metadata property.

    ``` yaml
    ...
    # ID of the Workers KV namespace (required)
    - name: kvNamespaceID
    value: "<YOUR_KV_ID>"
    ...
    ```

4. Get the ID of your Cloudflare account. You can find this in your browser’s URL bar after logging into the [Cloudflare dashboard](https://dash.cloudflare.com/), with the ID being the hex string right after `dash.cloudflare.com`. Eg. if the URL is `https://dash.cloudflare.com/456789abcdef8b5588f3d134f74acdef`, your account ID is `456789abcdef8b5588f3d134f74acdef`. Copy this and paste it into the [connections/workers-kv.yaml](./connections/workers-kv.yaml) file in the `cfAccountID` metadata property.

    ``` yaml
    ...
    # ID of the Workers KV namespace (required)
    - name: cfAccountID
    value: <YOUR_CLOUDFLARE_ACCOUNT_ID>
    ...
    ```

4. Create a Cloudflare Personal Access Token with permission to create and edit Workers and Workers KV namespaces. You can create it from the [API Tokens](https://dash.cloudflare.com/profile/api-tokens) page by performing the following:

- Click on `Create token`.
- Select the `Edit Cloudflare Workers` template.
- Choose your Cloudflare account for `Account Resources`.
- Choose `All zones from an account` and your Cloudflare account for `Zone Resources`.
- Click on `Continue to Summary` to generate the token.
- Copy this value and paste it into the [connections/workers-kv.yaml](./connections/workers-kv.yaml) file in the `cfAPIToken` metadata property.

    ``` yaml
    ...
    # ID of the Workers KV namespace (required)
    - name: cfAPIToken
    value: <YOUR_CLOUDFLARE_API_TOKEN>
    ...
    ```

5. Generate an Ed25519 private key as described in the [Dapr docs](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-cloudflare-workerskv/#generate-an-ed25519-key-pair).

    ``` bash
    # On mac generate a private key with OpenSSL
    brew install openssl@3
    $(brew --prefix)/opt/openssl@3/bin/openssl genpkey -algorithm ed25519 -out private.pem
    ```

    This should have generated a file, `private.pem`. Copy the contents of the `private.pem` file and paste it into the [connections/workers-kv.yaml](./connnections/workers-kv.yaml) file in the `key` metadata property in the format below.

    ``` yaml
    ...
    # PEM-encoded private Ed25519 key (required)
    - name: key
    value: |
        -----BEGIN PRIVATE KEY-----
        <YOUR_PRIVATE_KEY>
        -----END PRIVATE KEY-----   
    ...
    ```

### Create a Diagrid Connection to Cloudflare Workers KV

The order system requires a Diagrid connection to be created for the Diagrid APIs to access the Cloudflare Workers KV store. The Diagrid connection `statestorekv` will establish connectivity from Diagrid Cloud to your externally deployed Cloudflare Workers KV store. This connection will be scoped specifically to allow access from the `order-processor-http` App ID for storing orders.

1. Import the Cloudflare Workers KV Dapr Component file into CRA to create a Connection resource.

    ``` bash
    cd ../connections
    diagrid connection apply --file workers-kv.yaml
    ```

2. Check the CRA connection to ensure its status is `Ready` by navigating to the CRA UI [Connections page](https://cra.diagrid.io/connections) and viewing the list of connections. Clicking in on the `statestorekv` connection allows you to view details on the Cloudflare Workers KV connection.

    ![CRA UI Cloudflare Workers Connection](../../images/js-cloudflare/JS-cloudflare-ui-connection.png)

### Deploy on Cloudflare Workers: Order Processor Service

Review the code for the Order processor service and update the environment variables.

1. Navigate back to the Order processor service directory.

    ```bash
    cd ../order-processor-http
    ```

2. Inspect the code in [worker.js](./order-processor-http/src/worker.js) and install the Node dependencies:

    ```bash
    npm install
    ```

3. Update the [vars] section of the Order processor service's [wrangler.toml](./order-processor-http/wrangler.toml) file to add the `ORDERPROCESSOR_API_TOKEN` and `DAPR_HOST` variables obtained in the previous section.

    ``` bash
    export ORDERPROCESSOR_API_TOKEN=`diagrid appid get order-processor-http -o json | jq -r .status.apiToken`
    export DAPR_HOST=`diagrid project get $CRA_PROJECT_NAME -o json | jq -r '.status.endpoints.http.url'`
    echo $DAPR_HOST
    echo $ORDERPROCESSOR_API_TOKEN
    ```

    ``` toml
    [vars]
    STORAGE_ACCOUNT_NAME = "statestorekv"
    PUBSUB_NAME = "pubsub"
    PUBSUB_TOPIC = "orders"
    DAPR_HTTP_PORT = 443
    DAPR_HOST = "<YOUR_HOST>"
    ORDERPROCESSOR_API_TOKEN = "<YOUR_API_TOKEN>"
    ```

4. Deploy the Order processor worker to Cloudflare.

    ``` bash
    wrangler deploy
    ```

    The expected output should be something similar to the following:

    ``` bash
    Your worker has access to the following bindings:
    - Vars:
    - STORAGE_ACCOUNT_NAME: "statestorekv"
    - PUBSUB_NAME: "pubsub"
    - PUBSUB_TOPIC: "orders"
    - DAPR_HTTP_PORT: 443
    - DAPR_HOST: "https://http-prj1577.api.stg.cloud.di..."
    - ORDERPROCESSOR_API_TOKEN: "diagrid://v1/b040c94b-b9d2-4f8b-b9c3-..."
    Total Upload: 2.81 KiB / gzip: 1.01 KiB
    Uploaded order-processor-http (1.03 sec)
    Published order-processor-http (0.25 sec)
    https://order-processor-http.alice-f0d.workers.dev
    Current Deployment ID: bd4c9451-7ede-4fd6-97ba-a82eb386a112
    ```

5. Copy the second to last line of the output and set it as the `APP_ENDPOINT` environment variable.

    ```bash
    export APP_ENDPOINT=https://order-processor-http...
    ```

## Configure CRA Pub/Sub subscription

In order to receive messages using the CRA Pub/Sub API, the `order-processor-http` App ID needs to be updated with the following:

1. The application endpoint of the order processor application in Cloudflare for message delivery.
1. A Pub/Sub subscription notifying the CRA platform which messages should be delivered to the order processor app and on which route.

### Update the Application ID with the App Endpoint

1. Update the existing Order processor App Id.

    ``` bash
    diagrid appid update order-processor-http --app-endpoint $APP_ENDPOINT

    # Check that the appid status is ready
    diagrid appid get order-processor-http
    ```

### Create CRA Subscription

The subscription targets the Pub/Sub connection created above to the Diagrid-managed message broker, telling CRA to deliver messages that arrive on the specified topic to the application represented in CRA by the `order-processor-http` App ID on the `/orders` route.

1. Create a CRA subscription that is scoped to the Order processor (subscriber) app.

    ```bash
    # Ensure the previous environment variables are still set
    CRA_PROJECT_NAME="default" # Update with your project name
    PUBSUB_NAME="pubsub"
    PUBSUB_TOPIC="orders"

    diagrid subscription create $CRA_PROJECT_NAME --connection $PUBSUB_NAME --topic $PUBSUB_TOPIC --route /orders --scopes order-processor-http
    ```

2. Ensure the status of the subscription is ready.

    ```bash
    diagrid subscription get $CRA_PROJECT_NAME --project $CRA_PROJECT_NAME
    ```

## Test the Solution

1. Navigate to the Checkout service worker URL in the browser. This URL can be found by viewing the [Cloudflare dashboard](https://dash.cloudflare.com/) and clicking on the `Workers and Pages` section. Clicking into the `checkout-http` worker will display a link to the URL (eg. https://checkout-http.alice-f0d.workers.dev/) that will open in a new tab.

    Refresh the page to see a message like the following: 
    
    ```json
    Successfully published a message! Response: 204 No Content 
    {
        "orderId": 1,
        "time": "2023-07-10T15:02:33.993Z"
    }
    ```

2. In a new tab, navigate to the Order processor service worker URL in the browser. This URL can be found by viewing the [Cloudflare dashboard](https://dash.cloudflare.com/) and clicking on the `Workers and Pages` section. Clicking into the `order-processor-http` worker will display a link to the URL (eg. https://order-processor-http.alice-f0d.workers.dev/) that will open in a new tab.

    Refresh the page to see a list of the most recent orders that have arrived to the worker. Entries with a key but without data mean there is no data stored for that key in the KV store.

    ```json
    Orders received: 

    [
        {
        "key": "1",
        "data": {
            "orderId": 1,
            "time": "2023-07-10T15:02:33.993Z"
            }
        },
        {
        "key": "2"
        },
        {
        "key": "3"
        },
        {
        "key": "4"
        },
        {
        "key": "5"
        },
        {
        "key": "6"
        },
        {
        "key": "7"
        },
        {
        "key": "8"
        },
        {
        "key": "9"
        },
        {
        "key": "10"
        }
    ]
    ```

3. Send a sequence of 10 orders by appending `/sendOrders` to the url of the `checkout-http` worker (e.g. https://checkout-http.alice-f0d.workers.dev/sendOrders):

    ```json
    Successfully published these messages: 

    [
        {
            "orderId": 1,
            "time": "2023-07-10T15:10:05.890Z"
        },
        {
            "orderId": 2,
            "time": "2023-07-10T15:10:06.447Z"
        },
        {
            "orderId": 3,
            "time": "2023-07-10T15:10:06.931Z"
        },
        {
            "orderId": 4,
            "time": "2023-07-10T15:10:07.109Z"
        },
        {
            "orderId": 5,
            "time": "2023-07-10T15:10:07.541Z"
        },
        {
            "orderId": 6,
            "time": "2023-07-10T15:10:07.974Z"
        },
        {
            "orderId": 7,
            "time": "2023-07-10T15:10:08.427Z"
        },
        {
            "orderId": 8,
            "time": "2023-07-10T15:10:08.856Z"
        },
        {
            "orderId": 9,
            "time": "2023-07-10T15:10:09.021Z"
        },
        {
            "orderId": 10,
            "time": "2023-07-10T15:10:09.171Z"
        }
    ]
    ```

4. You can verify that the 10 orders have been stored in Cloudflare Workers KV by either going to the url of the `order-processor-http` worker or by using the Cloudflare dashboard and navigating to Workers & Pages -> KV and then clicking on `View` for the `order-processor-http-statestorekv` namespace.

    ![Cloudflare KV namespace](../../images/js-cloudflare/JS-cloudflare-kv-ns.png)

    ![Cloudflare KV items](../../images/js-cloudflare/JS-cloudflare-kv-items.png)


## Clean Up Resources

### Removing the Cloudflare resources

1. Using wrangler, list the Cloudflare Workers KV namespaces and identify the namespace ID for the `statestorekv` namespace:

    ```bash
    wrangler kv:namespace list
    ```

2. Delete the Cloudflare Workers KV namespace by specifying the namespace ID:

    ```bash
    wrangler kv:namespace delete --namespace-id <YOUR_KV_NAMESPACE_ID>
    ```
3. Navigate to the `samples/javascript/checkout-http` directory and use wrangler to delete the `checkout-http` worker:

    ```bash
    wrangler delete
    ``` 
    
    Type `Y`

4. Navigate to the `samples/javascript/order-processor-http` directory and use wrangler to delete the `order-processor-http` worker:

    ```bash
    wrangler delete
    ``` 
    
    Type `Y`

5. Since wrangler wasn't used for the `mydaprkv` worker, this worker needs to be deleted via the Cloudflare dashboard. Click on the `mydaprkv` worker in the dashboard, click `Manage Application`, scroll down to the bottom of the settings page, click `Delete`, and confirm the deletion.

### Removing the Diagrid resources

1. Delete the CRA project:

    ```bash
    diagrid project delete $CRA_PROJECT_NAME
    ```

    Type `Y` and enter.
