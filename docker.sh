PORT=${PORT:-9000}

docker build -t onbase-docs-api .

docker run --detach \
    -it \
    -p ${PORT}:9000 \
    --mount type=bind,source="$(pwd)/OnBaseDocsApi/api-config.json",target=/usr/src/app/OnBaseDocsApi/api-config.json \
    --name onbase-docs-api \
    onbase-docs-api
