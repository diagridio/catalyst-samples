import express from 'express';
import bodyParser from 'body-parser';
import { client as daprClient } from './dapr.js';

const appPort = process.env.PORT || 5001;
const kvName = process.env.KVSTORE_NAME || "kvstore";

const app = express()

app.use(bodyParser.json({ type: '*/*' }))

app.post('/kv/orders', async function(req, res) {
  req.accepts('application/json')

  const keyName = "order" + req.body.orderId
  const state = [
    {
      key: keyName,
      value: req.body
    }]

  try {
    await daprClient.state.save(kvName, state);
    console.log("Order saved successfully: " + req.body.orderId);
    res.sendStatus(200);
  } catch (error) {
    console.log("Error saving order: " + req.body.orderId);
    res.status(500).send(error);
  }
});

app.get('/kv/orders/:orderId', async function(req, res) {
  const keyName = "order" + req.params.orderId
  try {
    const order = await daprClient.state.get(kvName, keyName)
    console.log("Retrieved order: ", order)
    res.json(order)
  } catch (error) {
    console.log("Error retrieving order: " + req.params.orderId);
    res.status(500).send(error);
  }
});

app.delete('/kv/orders/:orderId', async function(req, res) {
  const keyName = "order" + req.params.orderId
  try {
    await daprClient.state.delete(kvName, keyName)
    console.log("Deleted order: ", req.params.orderId)
    res.sendStatus(200);
  } catch (error) {
    console.log("Error deleting order: " + req.params.orderId);
    res.status(500).send(error);
  }
});

app.listen(appPort, () => console.log(`server listening at :${appPort}`));
