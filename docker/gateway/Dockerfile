# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY src/FileManagement.Gateway/FileManagement.Gateway.csproj \
    src/FileManagement.Gateway/

RUN dotnet restore \
    src/FileManagement.Gateway/FileManagement.Gateway.csproj

COPY src/FileManagement.Gateway/ \
    src/FileManagement.Gateway/

RUN dotnet publish \
    src/FileManagement.Gateway/FileManagement.Gateway.csproj \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

RUN apk add --no-cache curl && \
    addgroup -S appgroup && \
    adduser -S appuser -G appgroup

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY \
    --from=build \
    --chown=appuser:appgroup \
    /app/publish \
    .

USER appuser

ENTRYPOINT ["dotnet", "FileManagement.Gateway.dll"]
