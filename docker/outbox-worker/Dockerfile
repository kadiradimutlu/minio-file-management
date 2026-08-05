# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY src/FileManagement.Domain/FileManagement.Domain.csproj \
    src/FileManagement.Domain/

COPY src/FileManagement.Contracts/FileManagement.Contracts.csproj \
    src/FileManagement.Contracts/

COPY src/FileManagement.Application/FileManagement.Application.csproj \
    src/FileManagement.Application/

COPY src/FileManagement.Infrastructure/FileManagement.Infrastructure.csproj \
    src/FileManagement.Infrastructure/

COPY src/FileManagement.Outbox.Worker/FileManagement.Outbox.Worker.csproj \
    src/FileManagement.Outbox.Worker/

RUN dotnet restore \
    src/FileManagement.Outbox.Worker/FileManagement.Outbox.Worker.csproj

COPY src/FileManagement.Domain/ \
    src/FileManagement.Domain/

COPY src/FileManagement.Contracts/ \
    src/FileManagement.Contracts/

COPY src/FileManagement.Application/ \
    src/FileManagement.Application/

COPY src/FileManagement.Infrastructure/ \
    src/FileManagement.Infrastructure/

COPY src/FileManagement.Outbox.Worker/ \
    src/FileManagement.Outbox.Worker/

RUN dotnet publish \
    src/FileManagement.Outbox.Worker/FileManagement.Outbox.Worker.csproj \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

RUN groupadd --system appgroup && \
    useradd \
        --system \
        --gid appgroup \
        --create-home \
        --home-dir /home/appuser \
        appuser

WORKDIR /app

COPY \
    --from=build \
    --chown=appuser:appgroup \
    /app/publish \
    .

USER appuser

ENTRYPOINT ["dotnet", "FileManagement.Outbox.Worker.dll"]