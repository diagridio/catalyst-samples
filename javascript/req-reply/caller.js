import express from 'express';
import bodyParser from 'body-parser';
import axios from "axios";

const appPort = process.env.PORT || 5001;
const daprApiToken = process.env.DAPR_API_TOKEN || "";
const daprHttpEndpoint = process.env.DAPR_HTTP_ENDPOINT || "http://localhost";
const invokeTargetAppID = process.env.INVOKE_APPID || "target";

const app = express()

app.use(bodyParser.json({ type: '*/*' }))

app.post('/invoke/orders', async function(req, res) {
  let config = {
    headers: {
      "dapr-app-id": invokeTargetAppID,
      "dapr-api-token": daprApiToken
    }
  };
  let order = req.body

  try {
    await axios.post(`${daprHttpEndpoint}/invoke/neworders`, order, config);
    console.log("Invocation successful with status code: %d ", res.statusCode);
    res.sendStatus(200);
  } catch (error) {
    console.log("Error invoking app at " + `${daprHttpEndpoint}/invoke/neworders`);
    res.status(500).send(error);
  }
});

app.listen(appPort, () => console.log(`server listening at :${appPort}`));
