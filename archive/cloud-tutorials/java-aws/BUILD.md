
### Checkout build and push

```
aws configure
aws ecr-public get-login-password --region us-east-1 | docker login --username AWS --password-stdin public.ecr.aws

mvn clean install 

docker buildx build --platform linux/amd64 -t checkout/myapp:latest . 
docker run -it -p8081:8080  checkout:latest
docker images
docker tag 919bec0df034 public.ecr.aws/o3d2i4j6/dapr/checkout
docker push public.ecr.aws/o3d2i4j6/dapr/checkout

```

### Order processor build and push

```
docker buildx build --platform linux/amd64 -t order-processor/myapp:latest . 


docker run -it -p8080:8080 order-processor/myapp:latest
curl http://localhost:8080/actuator/health

docker images

docker tag 01745c0a31bf public.ecr.aws/o3d2i4j6/dapr/order-processor
docker push public.ecr.aws/o3d2i4j6/dapr/order-processor

```
 