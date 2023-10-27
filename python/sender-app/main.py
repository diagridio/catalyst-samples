from dapr.clients import DaprClient
from fastapi import FastAPI
from pydantic import BaseModel, Json
from cloudevents.sdk.event import v1
import logging
import grpc
# import httpx

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


@app.post("/subscribe")
def receive_messages(event: CloudEvent):
    print('Subscriber received : %s' % event.data['orderId'], flush=True)
    return {'success': True}

# logging.info('Order requested: ' + str(orderId)) logging.info('Result: ' + str(result))

# @app.post('/request')
# async def send_request(order: Order):
#     try:
#         result = httpx.post(
#             url='%s/reply' % ("https://"),
#             data=order.model_dump_json,
#             headers=headers
#         )
#         logging.info('Request Successful. Order sent: %s' %
#                      order.orderId)
#         return {'success': True}
#     except grpc.RpcError as err:
#         logging.info(f"ErrorCode={err.code()}")


# @app.post("/reply")
# def receive_request(order: Order):
#     logging.info('Order received : ' + order.model_dump_json, flush=True)
#     return {'success': True}


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
