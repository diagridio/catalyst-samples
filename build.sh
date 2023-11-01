# aws sso login
# aws ecr-public get-login-password --region us-east-1 | docker login --username AWS --password-stdin public.ecr.aws/d3f9w4q8

cd csharp
docker buildx build --platform linux/amd64 -t catalyst-samples/csharp .
docker tag catalyst-samples/csharp:latest public.ecr.aws/d3f9w4q8/catalyst-samples/csharp:latest
docker push public.ecr.aws/d3f9w4q8/catalyst-samples/csharp:latest


cd ../python
docker buildx build --platform linux/amd64 -t catalyst-samples/python .
docker tag catalyst-samples/python:latest public.ecr.aws/d3f9w4q8/catalyst-samples/python:latest
docker push public.ecr.aws/d3f9w4q8/catalyst-samples/python:latest

cd ../javascript
docker buildx build --platform linux/amd64 -t catalyst-samples/javascript .
docker tag catalyst-samples/javascript:latest public.ecr.aws/d3f9w4q8/catalyst-samples/javascript:latest
docker push public.ecr.aws/d3f9w4q8/catalyst-samples/javascript:latest

cd ../java
docker buildx build --platform linux/amd64 -t catalyst-samples/java .
docker tag catalyst-samples/java:latest public.ecr.aws/d3f9w4q8/catalyst-samples/java:latest
docker push public.ecr.aws/d3f9w4q8/catalyst-samples/java:latest