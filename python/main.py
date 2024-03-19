from dapr.clients import DaprClient
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from cloudevents.sdk.event import v1
import logging
import grpc
import requests
import os

from opentelemetry import trace
from opentelemetry.sdk.trace.export import ConsoleSpanExporter, BatchSpanProcessor
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.instrumentation.grpc import GrpcInstrumentorClient

app = FastAPI()

FastAPIInstrumentor.instrument_app(app)

logging.basicConfig(level=logging.INFO)

class Order(BaseModel):
    orderId: str
    productId: int
    quantity: int

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

trace.set_tracer_provider(TracerProvider())
trace.get_tracer_provider().add_span_processor(BatchSpanProcessor(ConsoleSpanExporter()))
# Get the tracer
tracer = trace.get_tracer(__name__)

grpc_client_instrumentor = GrpcInstrumentorClient()
grpc_client_instrumentor.instrument()

# Set up required inputs for http client to perform service invocation
http_endpoint = os.getenv('DAPR_HTTP_ENDPOINT', 'http://localhost')
grpc_endpoint = os.getenv('DAPR_GRPC_ENDPOINT', 'http://localhost')
dapr_api_token = os.getenv('DAPR_API_TOKEN', '')
pubsub_name = os.getenv('PUBSUB_NAME', 'pubsub')
kvstore_name = os.getenv('KVSTORE_NAME', 'kvstore')
invoke_target_appid = os.getenv('INVOKE_APPID', 'target')

@app.get('/')
async def helloworld():
    return {"DAPR_HTTP_ENDPOINT": http_endpoint,
            "DAPR_GRPC_ENDPOINT": grpc_endpoint,
            "DAPR_API_TOKEN": dapr_api_token,
            "PUBSUB_NAME": pubsub_name,
            "KVSTORE_NAME": kvstore_name,
            "INVOKE_APPID": invoke_target_appid}

@app.post('/pubsub/orders')
async def publish_orders(order: Order):
    with tracer.start_as_current_span(name='publish') as span:
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
                logging.error(
                    f"Error occurred while publishing order: {err.code()}")

@app.post('/pubsub/neworders')
def consume_orders(event: CloudEvent):
    logging.info('Order #%s received' % event.data['orderId'])
    with tracer.start_as_current_span(name='neworder') as span:
        with DaprClient() as d:
            try:
                d.save_state(store_name=kvstore_name,
                             key=str(event.data['orderId']), value=str(event.data))
                return {"success": True}
            except grpc.RpcError as err:
                print(f"Error={err.details()}")
                raise HTTPException(status_code=500, detail=err.details())
        return {'success': True}

@app.post('/invoke/orders')
async def send_order(order: Order):
    headers = {'dapr-app-id': invoke_target_appid, 'dapr-api-token': dapr_api_token,
               'content-type': 'application/json'}
    try:
        result = requests.post(
            url='%s/invoke/neworders' % (http_endpoint),
            data=order.model_dump_json(),
            headers=headers
        )

        if result.ok:
            logging.info('Invocation successful with status code: %s' %
                         result.status_code)
            return str(order)
        else:
            logging.error(
                'Error occurred while invoking App ID: %s' % result.reason)
            raise HTTPException(status_code=500, detail=result.reason)

    except grpc.RpcError as err:
        logging.error(f"ErrorCode={err.code()}")
        raise HTTPException(status_code=500, detail=err.details())


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
            print(f"Error={err.details()}")
            raise HTTPException(status_code=500, detail=err.details())

@app.get('/kv/orders/{orderId}')
def get_kv(orderId: int):
    with DaprClient() as d:
        try:
            kv = d.get_state(kvstore_name, str(orderId))
            return {"data": kv.data}
        except grpc.RpcError as err:
            print(f"Error={err.details()}")
            raise HTTPException(status_code=500, detail=err.details())

@app.delete('/kv/orders/{orderId}')
def delete_kv(orderId: int):
    with DaprClient() as d:
        try:
            d.delete_state(kvstore_name, str(orderId))
            return {'success': True}
        except grpc.RpcError as err:
            print(f"Error={err.details()}")
            raise HTTPException(status_code=500, detail=err.details())
