from dapr.clients import DaprClient
from fastapi import FastAPI
from pydantic import BaseModel, Json
from cloudevents.sdk.event import v1
import logging
import grpc

app = FastAPI()

logging.basicConfig(level=logging.INFO)


class Order(BaseModel):
    orderId: str


class CloudEvent(BaseModel):
    datacontenttype: str
    source: str
    topic: str
    pubsubname: str
    data: dict
    id: str
    specversion: str
    tracestate: str
    type: str
    traceid: str


@app.get('/')
async def helloworld():
    return {"Hello World"}


@app.post("/consume")
def consume(event: CloudEvent):
    print('Message received : %s' % event.data['orderId'], flush=True)
    return {'success': True}


@app.post("/receiverequest")
def receive_request(order: Order):
    logging.info('Order received : ' + order.model_dump_json, flush=True)
    return {'success': True}
