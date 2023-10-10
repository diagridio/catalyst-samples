from dapr.clients import DaprClient
from fastapi import FastAPI
from pydantic import BaseModel, Json
import logging
import grpc

logging.basicConfig(level=logging.INFO)

app = FastAPI()


class Order(BaseModel):
    orderId: str


class CloudEvent(BaseModel):
    datacontenttype: str
    id: str
    pubsubname: str
    source: str
    specversion: str
    topic: str
    traceid: str
    traceparent: str
    tracestate: str
    type: str


@app.get('/')
async def read_root():
    return {"Hello World"}


@app.post('/publish')
async def order_publisher(order: Order):
    with DaprClient() as d:
        result = d.publish_event(
            pubsub_name='test-test',
            topic_name='orders',
            data=order.orderId,
            data_content_type='application/json',
        )
        logging.info('Publish Successful. Order published: %s' %
                     order.orderId)
        return {'success': True}


@app.post('/subscribe')
def orders_subscriber(event: CloudEvent):
    print('Subscriber received : %s' % event.data['orderId'], flush=True)
    return {'success': True}


@app.post('/savekv')
def create_kv(order: Order):
    with DaprClient() as d:
        try:
            d.save_state(store_name='kvstore',
                         key=order.orderId, value=str(order))
            return {"success": True}
        except grpc.RpcError as err:
            print(f"ErrorCode={err.code()}")


@app.get('/getkv')
def get_kv(order: Order):
    with DaprClient() as d:
        kv = d.get_state("kvstore", order.orderId)
        return kv.data


@app.delete('/deletekv')
def delete_kv(order: Order):
    with DaprClient() as d:
        kv = d.delete_state("kvstore", order.orderId)
        return {'success': True}
