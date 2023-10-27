import express from 'express';
import bodyParser from 'body-parser';
import axios from "axios";
import { DaprClient } from "@dapr/dapr";

const daprApiToken = process.env.DAPR_API_TOKEN;
const daprHttpEndpoint = process.env.DAPR_HTTP_ENDPOINT;
const appPort = process.env.PORT || 5000; 

const app = express()

const client = new DaprClient({daprApiToken: daprApiToken});

app.use(bodyParser.json({ type: '*/*' })) 

//#region Pub/Sub API

app.post('/publish', async function (req, res) {
    let order = req.body
    //Publish an event using Dapr pub/sub
    await client.pubsub.publish("pubsub", "orders", order);
    console.log("Published data: " + order.orderId);
    res.status(200)
});

app.post('/consume', (req, res) => {
  console.log("Message received: " + JSON.stringify(req.body.data))
  res.sendStatus(200);
});

//#endregion

//#region Request/Reply API 

app.post('/sendrequest', async function (req, res) {
  let config = {
    headers: {
        "dapr-app-id": "target",
        "dapr-api-token": daprApiToken
    }
  };
  let order = req.body
  
  await axios.post(`${daprHttpEndpoint}/receiverequest`, order, config).then(res => console.log("Invocation successful with status code: %d ", res.status)).catch(err => console.log(err))
});

app.post('/receiverequest', (req, res) => {
  console.log("Request received: %s", JSON.stringify(req.body))
  res.sendStatus(200);
});

//#endregion

//#region Key/Value API

app.post('/savekv', async function (req, res) {
  req.accepts('application/json')
  const state = [
    {
        key: req.body.orderId,
        value: req.body
    }]

    await client.state.save("kvstore", state)
    console.log("Order saved ")
});

app.post('/getkv', async function(req, res){
  req.accepts('application/json')
  var kv = await client.state.get("kvstore", req.body.orderId)
  console.log("Retrieved Order: ", kv)
});

app.post('/deletekv', async function (req, res) {
  req.accepts('application/json')
  await client.state.delete("kvstore", req.body.orderId)
  console.log("Deleted Order: ", req.body.orderId)
});

//#endregion


app.listen(appPort);
