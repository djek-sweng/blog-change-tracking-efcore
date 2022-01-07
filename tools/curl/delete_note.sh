#!/bin/sh

ID=$1

curl -X 'DELETE' \
  'https://localhost:5001/api/notes/'$ID \
  -H 'accept: */*'
