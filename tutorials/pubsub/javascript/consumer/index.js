import express from 'express';
import bodyParser from 'body-parser';
import axios from "axios";
import { DaprClient } from "@dapr/dapr";

const port = 9000;
const app = express()

const communicationProtocol = "http"
const daprHost = process.env.DAPR_HOST ?? "https://http-prj8812.api.stg.cloud.diagrid.io"
const daprPort = 443 
const daprApiToken = "diagrid://v1/04b746d3-2fd5-40a0-bbe6-92d5fb73967b/8812/default/test4/16451821-b690-41e8-9457-4d510b713656"
const client = new DaprClient({daprHost, daprPort, communicationProtocol, daprApiToken})

app.use(bodyParser.json({ type: '*/*' })) 

//#region Pub/Sub API

app.post('/publish', async function (req, res) {
    let order = req.body
    //Publish an event using Dapr pub/sub
    await client.pubsub.publish("fake", "orders", order);
    console.log("Published data: " + order.orderId);
    res.status(200)
});


// Dapr subscription routes orders topic to this route
app.post('/subscribe', (req, res) => {
    console.log("Message received: " + JSON.stringify(req.body.data))
    res.sendStatus(200);
});

//#endregion

//#region Request/Reply API 

app.post('/request', async function (req, res) {
  let config = {
    headers: {
        "dapr-app-id": "javascript"
    }
  };
  let order = req.body
  const response = await axios.post(`${daprHost}:${daprPort}/reply`, order, config).then(res => console.log(res)).catch(err => console.log(err))
  console.log("Published data: " + response.data.config);
  res.status(200)
});

// Dapr subscription routes orders topic to this route
app.post('/reply', (req, res) => {
  console.log("Invocation received: " + JSON.stringify(req.body.data))
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

app.get('/getkv', async function(req, res){
  req.accepts('application/json')
  var kv = await client.state.get("kvstore", req.body.orderId)
  console.log("Retrieved Order: ", kv)
});

app.delete('/deletekv', async function (req, res) {
  req.accepts('application/json')
  await client.state.delete("kvstore", req.body.orderId)
  console.log("Deleted Order: ", req.body.orderId)
});

//#endregion


app.listen(port);
