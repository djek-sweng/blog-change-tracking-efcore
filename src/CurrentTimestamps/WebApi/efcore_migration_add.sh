#!/bin/sh

MIGRATION_NAME=$1

dotnet ef migrations add $MIGRATION_NAME \
    --context "DatabaseContext" \
    --project "./../Core/Core.csproj"
