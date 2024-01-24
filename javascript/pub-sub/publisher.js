import express from 'express';
import bodyParser from 'body-parser';
import { client as daprClient } from './dapr.js';

const appPort = process.env.PORT || 5001;
const pubSubName = process.env.PUBSUB_NAME || "pubsub";

const app = express()

app.use(bodyParser.json({ type: '*/*' }))

app.post('/pubsub/orders', async function(req, res) {
  let order = req.body
  try {
    await daprClient.pubsub.publish(pubSubName, "orders", order);
    console.log("Published data: " + order.orderId);
    res.sendStatus(200);
  } catch (error) {
    console.log("Error publishing order: " + order.orderId);
    console.error(error)
    res.status(500).send(error);
  }
});

app.listen(appPort, () => console.log(`server listening at :${appPort}`));
