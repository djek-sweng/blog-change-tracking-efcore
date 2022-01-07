#!/bin/sh

# set variables
ASPNETCORE_ENVIRONMENT="Development"
SLN_FILE="./src/CurrentTimestamps/CurrentTimestamps.sln"
CSPORJ_FILE="./src/CurrentTimestamps/WebApi/WebApi.csproj"

# build and run .NET web-api
dotnet build $SLN_FILE --no-incremental --force
dotnet run --project $CSPORJ_FILE --no-build
