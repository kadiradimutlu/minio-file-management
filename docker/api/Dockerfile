# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY src/FileManagement.Domain/FileManagement.Domain.csproj \
    src/FileManagement.Domain/

COPY src/FileManagement.Application/FileManagement.Application.csproj \
    src/FileManagement.Application/

COPY src/FileManagement.Infrastructure/FileManagement.Infrastructure.csproj \
    src/FileManagement.Infrastructure/

COPY src/FileManagement.Api/FileManagement.Api.csproj \
    src/FileManagement.Api/

RUN dotnet restore \
    src/FileManagement.Api/FileManagement.Api.csproj

COPY src/ src/

RUN dotnet publish \
    src/FileManagement.Api/FileManagement.Api.csproj \
    --configuration "${BUILD_CONFIGURATION}" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

RUN apk add --no-cache curl krb5-libs && \
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

ENTRYPOINT ["dotnet", "FileManagement.Api.dll"]