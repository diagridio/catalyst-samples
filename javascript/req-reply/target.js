import express from 'express';
import bodyParser from 'body-parser';

const appPort = process.env.PORT || 5001;

const app = express()

app.use(bodyParser.json({ type: '*/*' }))

app.post('/invoke/neworders', (req, res) => {
  console.log("Request received: %s", JSON.stringify(req.body))
  res.sendStatus(200);
});

app.listen(appPort, () => console.log(`server listening at :${appPort}`));
