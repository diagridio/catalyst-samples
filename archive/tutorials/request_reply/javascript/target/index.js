import express from 'express';
import bodyParser from 'body-parser';
import axios from "axios";
import { DaprClient } from "@dapr/dapr";

const port = 9001;
const app = express()

const client = new DaprClient();

app.use(bodyParser.json({ type: '*/*' })) 

//#region Pub/Sub API

app.post('/publish', async function (req, res) {
    let order = req.body
    //Publish an event using Dapr pub/sub
    await client.pubsub.publish("pubsub", "orders", order);
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

app.post('/sendrequest', async function (req, res) {
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
