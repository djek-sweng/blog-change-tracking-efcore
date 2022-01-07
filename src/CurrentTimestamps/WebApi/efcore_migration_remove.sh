#!/bin/sh

dotnet ef migrations remove \
    --context "DatabaseContext" \
    --project "./../Core/Core.csproj" \
    --force
