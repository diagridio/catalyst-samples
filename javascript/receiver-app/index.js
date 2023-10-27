import express from 'express';
import bodyParser from 'body-parser';
import axios from "axios";
import { DaprClient } from "@dapr/dapr";

const port = 9000;
const app = express()

const client = new DaprClient();

app.use(bodyParser.json({ type: '*/*' })) 

//#region Pub/Sub API

// Dapr subscription routes orders topic to this route
app.post('/consume', (req, res) => {
    console.log("Message received: " + JSON.stringify(req.body.data))
    res.sendStatus(200);
});

//#endregion

//#region Request/Reply API 

// Dapr subscription routes orders topic to this route
app.post('/receiverequest', (req, res) => {
  console.log("Invocation received: " + JSON.stringify(req.body.data))
  res.sendStatus(200);
});

//#endregion

app.listen(port);
