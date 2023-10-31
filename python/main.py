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
pubsub_name = os.getenv('PUBSUB_NAME', 'pubsub')
kvstore_name = os.getenv('KVSTORE_NAME', 'kvstore')


@app.get('/')
async def helloworld():
    return {"Hello World"}


@app.post('/pubsub/orders')
async def publish_orders(order: Order):
    with DaprClient() as d:
        try:
            result = d.publish_event(
                pubsub_name=pubsub_name,
                topic_name='orders',
                data=order.model_dump_json(),
                data_content_type='application/json',
            )
            logging.info('Publish Successful. Order published: %s' %
                         order.orderId)
            return {'success': True}
        except grpc.RpcError as err:
            print(f"Error occurred while publishing order: {err.code()}")


@app.post('/pubsub/neworders')
def consume_orders(event: CloudEvent):
    logging.info('Order received : %s' % event.data['orderId'], flush=True)
    return {'success': True}


@app.post('/invoke/orders')
async def send_order(order: Order):
    headers = {'dapr-app-id': 'target', 'dapr-api-token': dapr_api_token,
               'content-type': 'application/json'}
    try:
        result = requests.post(
            url='%s/invoke/neworders' % (base_url),
            data=order.model_dump_json(),
            headers=headers
        )

        if result.ok:
            logging.info('Invocation successful with status code: %s' %
                         result.status_code)
        else:
            logging.error(
                'Error occurred while invoking App ID: %s' % result.reason)

        return str(order)
    except grpc.RpcError as err:
        logging.error(f"ErrorCode={err.code()}")


@app.post('/invoke/neworders')
def receive_order(order: Order):
    logging.info('Request received : ' + str(order))
    return str(order)


@app.post('/kv/orders')
def create_kv(order: Order):
    with DaprClient() as d:
        try:
            d.save_state(store_name=kvstore_name,
                         key=str(order.orderId), value=str(order))
            return {"success": True}
        except grpc.RpcError as err:
            print(f"Error={err.code()}")


@app.get('/kv/orders/{orderId}')
def get_kv(orderId: int):
    with DaprClient() as d:
        kv = d.get_state(kvstore_name, str(orderId))
        return kv.data


@app.delete('/kv/orders/{orderId}')
def delete_kv(orderId: int):
    with DaprClient() as d:
        kv = d.delete_state(kvstore_name, str(orderId))
        return {'success': True}
