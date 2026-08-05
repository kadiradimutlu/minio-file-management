# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY src/FileManagement.Contracts/FileManagement.Contracts.csproj src/FileManagement.Contracts/
COPY src/FileManagement.Operations.Worker/FileManagement.Operations.Worker.csproj src/FileManagement.Operations.Worker/

RUN dotnet restore src/FileManagement.Operations.Worker/FileManagement.Operations.Worker.csproj

COPY src/FileManagement.Contracts/ src/FileManagement.Contracts/
COPY src/FileManagement.Operations.Worker/ src/FileManagement.Operations.Worker/

RUN dotnet publish src/FileManagement.Operations.Worker/FileManagement.Operations.Worker.csproj \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

RUN groupadd --system appgroup && \
    useradd --system --gid appgroup --create-home --home-dir /home/appuser appuser

WORKDIR /app

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "FileManagement.Operations.Worker.dll"]
