# OnBase Documents API

## Configure

Copy `OnBaseDocsApi/api-config-example.yaml` to `OnBaseDocsApi/api-config.yaml` and modify as necessary

## Run the API

Example Dockerfile:

```Dockerfile
FROM mono:6.6

WORKDIR /usr/src/app

COPY . .

RUN apt-get -y update && apt-get -y install mono-xsp4

RUN nuget restore
RUN msbuild -p:Configuration=Release OnBaseDocsApi

CMD cd OnBaseDocsApi && xsp4 --port=${PORT} --nonstop
```
