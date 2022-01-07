#!/bin/sh

ID=$1

curl -X 'GET' \
  'https://localhost:5001/api/notes/'$ID \
  -H 'accept: */*'
