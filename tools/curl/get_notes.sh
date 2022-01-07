#!/bin/sh

curl -X 'GET' \
  'https://localhost:5001/api/notes' \
  -H 'accept: */*'
