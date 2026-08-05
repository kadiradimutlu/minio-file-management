# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY src/FileManagement.Identity.Infrastructure/FileManagement.Identity.Infrastructure.csproj \
    src/FileManagement.Identity.Infrastructure/

COPY src/FileManagement.Identity.Api/FileManagement.Identity.Api.csproj \
    src/FileManagement.Identity.Api/

RUN dotnet restore \
    src/FileManagement.Identity.Api/FileManagement.Identity.Api.csproj

COPY src/FileManagement.Identity.Infrastructure/ \
    src/FileManagement.Identity.Infrastructure/

COPY src/FileManagement.Identity.Api/ \
    src/FileManagement.Identity.Api/

RUN dotnet publish \
    src/FileManagement.Identity.Api/FileManagement.Identity.Api.csproj \
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

ENTRYPOINT ["dotnet", "FileManagement.Identity.Api.dll"]
