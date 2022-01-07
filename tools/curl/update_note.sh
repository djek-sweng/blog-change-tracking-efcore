#!/bin/sh

ID=$1
MESSAGE=$2

curl -X 'PUT' \
  'https://localhost:5001/api/notes' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "id": "'$ID'",
  "message": "'$MESSAGE'"
}'
