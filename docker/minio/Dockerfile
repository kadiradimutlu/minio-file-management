# syntax=docker/dockerfile:1

FROM golang:1.24.8-alpine AS build

ARG MINIO_VERSION=RELEASE.2025-10-15T17-29-55Z

RUN apk add --no-cache git ca-certificates

WORKDIR /src/minio

RUN git clone \
    --branch "${MINIO_VERSION}" \
    --depth 1 \
    https://github.com/minio/minio.git \
    .

RUN mkdir -p /out && \
    LDFLAGS="$(go run buildscripts/gen-ldflags.go)" && \
    CGO_ENABLED=0 \
    GOOS=linux \
    go build \
        -tags kqueue \
        -trimpath \
        --ldflags "${LDFLAGS}" \
        -o /out/minio \
        .

RUN /out/minio --version

FROM alpine:3.22

ARG MINIO_VERSION

LABEL org.opencontainers.image.source="https://github.com/minio/minio"
LABEL org.opencontainers.image.version="${MINIO_VERSION}"

RUN apk add --no-cache ca-certificates curl

COPY --from=build /out/minio /usr/local/bin/minio

RUN mkdir -p /data

EXPOSE 9000 9001

ENTRYPOINT ["minio"]
CMD ["server", "/data", "--console-address", ":9001"]