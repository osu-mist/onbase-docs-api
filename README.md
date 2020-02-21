# OnBase Documents API

## Run the API

### Docker

Build

```shell
docker build -t onbase-docs-api .
```

Run
```shell
docker run --detach \
    -it \
    -p ${PORT}:9000 \
    --name onbase-docs-api \
    onbase-docs-api
```
