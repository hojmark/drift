FROM debian:trixie-slim

ENV Drift_ExecutionEnvironment="container"

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      iputils-ping iproute2 fping net-tools && \
    rm -rf /var/lib/apt/lists/* /var/cache/apt/* /var/log/* /tmp/*

LABEL "org.opencontainers.image.authors"="hojmark"
LABEL "org.opencontainers.image.description"="Monitor network drift against your declared state"
LABEL "org.opencontainers.image.licenses"="AGPL-3.0"
LABEL "org.opencontainers.image.source"="https://github.com/hojmark/drift"
LABEL "org.opencontainers.image.title"="Drift"
LABEL "org.opencontainers.image.url"="https://docker.io/hojmark/drift"
LABEL "org.opencontainers.image.vendor"="hojmark"

WORKDIR /app
COPY ./publish/linux-x64/drift .
ENTRYPOINT ["./drift"]
