import express from 'express';
import bodyParser from 'body-parser';
import axios from "axios";
import { DaprClient } from "@dapr/dapr";

const daprApiToken = process.env.DAPR_API_TOKEN;
const daprHttpEndpoint = process.env.DAPR_HTTP_ENDPOINT;
const appPort = process.env.PORT || 5000; 

const app = express()

const client = new DaprClient({daprHost: daprHttpEndpoint, daprPort: "443", daprApiToken: daprApiToken});

app.use(bodyParser.json({ type: '*/*' })) 

//#region Pub/Sub API

app.post('/publish', async function (req, res) {
    let order = req.body
    try {
      await client.pubsub.publish("pubsub", "orders", order);
      console.log("Published data: " + order.orderId);
      res.sendStatus(200);
    } catch (error){
      console.log("Error publishing order: " + order.orderId);
      res.status(500).send(error);
    }
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
  
  try {
    await axios.post(`${daprHttpEndpoint}/receiverequest`, order, config);
    console.log("Invocation successful with status code: %d ", res.status);
    res.sendStatus(200);
  } catch (error){
    console.log("Error invoking app at " + `${daprHttpEndpoint}/receiverequest`);
    res.status(500).send(error);
  }

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

    try {
      await client.state.save("kvstore", state);
      console.log("Order saved successfully: " + req.body.orderId);
      res.sendStatus(200);
    } catch (error){
      console.log("Error saving order: " + req.body.orderId);
      res.status(500).send(error);
    }
});

app.post('/getkv', async function(req, res){
  req.accepts('application/json')
  try {
    var kv = await client.state.get("kvstore", req.body.orderId)
    console.log("Retrieved order: ", kv)
    res.sendStatus(200);
  } catch (error){
    console.log("Error retrieving order: " + req.body.orderId);
    res.status(500).send(error);
  }
});

app.post('/deletekv', async function (req, res) {
  req.accepts('application/json')
  try {
    await client.state.delete("kvstore", req.body.orderId)
    console.log("Deleted order: ", req.body.orderId)
    res.sendStatus(200);
  } catch (error){
    console.log("Error deleting order: " + req.body.orderId);
    res.status(500).send(error);
  }
});

//#endregion


app.listen(appPort);
