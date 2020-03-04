# OnBase Documents API

## Configure

Copy `OnBaseDocsApi/api-config-example.yaml` to `OnBaseDocsApi/api-config.yaml` and modify as necessary

## Run the API

### Docker

Remove any existing containers named `onbase-docs-api`
```shell
docker rm -f onbase-docs-api
```

Build and run the API
```shell
./docker.sh
```
