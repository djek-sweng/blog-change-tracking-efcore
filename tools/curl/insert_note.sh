#!/bin/sh

MESSAGE=$1

curl -X 'POST' \
  'https://localhost:5001/api/notes' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "message": "'$MESSAGE'"
}'
