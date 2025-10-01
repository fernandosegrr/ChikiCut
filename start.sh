#!/bin/sh
set -e

dotnet restore

dotnet build --configuration Release

dotnet run --project ChikiCut.web/ChikiCut.web.csproj --urls=http://0.0.0.0:${PORT:-8080}
