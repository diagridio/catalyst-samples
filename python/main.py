from dapr.clients import DaprClient
from fastapi import FastAPI
from pydantic import BaseModel, Json
from cloudevents.sdk.event import v1
import logging
import grpc
import requests
import os

app = FastAPI()

logging.basicConfig(level=logging.INFO)


class Order(BaseModel):
    orderId: int


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


# Set up required inputs for http client to perform service invocation
base_url = os.getenv('DAPR_HTTP_ENDPOINT')
dapr_api_token = os.getenv('DAPR_API_TOKEN')


@app.get('/')
async def helloworld():
    return {"Hello World"}


@app.post('/publish')
async def publish_messages(order: Order):
    with DaprClient() as d:
        try:
            result = d.publish_event(
                pubsub_name='testpubsub',
                topic_name='orders',
                data=order.model_dump_json(),
                data_content_type='application/json',
            )
            logging.info('Publish Successful. Order published: %s' %
                         order.orderId)
            return {'success': True}
        except grpc.RpcError as err:
            print(f"ErrorCode={err.code()}")


@app.post("/orders")
def receive_messages(event: CloudEvent):
    print('Message received : %s' % event.data['orderId'], flush=True)
    return {'success': True}


@app.post('/sendrequest')
async def send_request(order: Order):
    headers = {'dapr-app-id': 'target', 'dapr-api-token': dapr_api_token, 'content-type': 'application/json'}
    try:
        result = requests.post(
            url='%s/receiverequest' % (base_url),
            data=order.model_dump_json(),
            headers=headers
        )

        if result.ok:
            logging.info('Invocation successful with status code: %s' % result.status_code)
        else:
            logging.error('Error occurred while invoking App ID: {%s' % result.reason)
        
        return str(order)
    except grpc.RpcError as err:
        logging.error(f"ErrorCode={err.code()}")


@app.post("/receiverequest")
def receive_request(order: Order):
    logging.info('Request received : ' + str(order))
    return str(order)


@app.post('/savekv')
def create_kv(order: Order):
    with DaprClient() as d:
        try:
            d.save_state(store_name='kvstore',
                         key=order.orderId, value=str(order))
            return {"success": True}
        except grpc.RpcError as err:
            print(f"Error={err.code()}")


@app.post('/getkv')
def get_kv(order: Order):
    with DaprClient() as d:
        kv = d.get_state("kvstore", order.orderId)
        return kv.data


@app.post('/deletekv')
def delete_kv(order: Order):
    with DaprClient() as d:
        kv = d.delete_state("kvstore", order.orderId)
        return {'success': True}
