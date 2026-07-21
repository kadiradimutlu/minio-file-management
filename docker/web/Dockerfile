# syntax=docker/dockerfile:1

FROM node:24-alpine AS build

WORKDIR /app

COPY src/FileManagement.Web/package.json ./
COPY src/FileManagement.Web/package-lock.json ./

RUN npm ci

COPY src/FileManagement.Web/ ./

ARG VITE_API_BASE_URL=/api

ENV VITE_API_BASE_URL="${VITE_API_BASE_URL}"

RUN npm run build

FROM nginx:alpine AS final

COPY docker/web/nginx.conf \
    /etc/nginx/conf.d/default.conf

COPY --from=build \
    /app/dist \
    /usr/share/nginx/html

EXPOSE 80