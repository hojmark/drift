FROM quay.io/fedora/fedora-minimal:42

ENV DRIFT_ENVIRONMENT="container"

RUN microdnf --setopt=install_weak_deps=False install -y \
    # iputils (ping), iproute (ip neigh), arp TODO remove arp
    iputils iproute fping arp && \
    dnf clean all && \
    rm -rf /var/cache /var/log /tmp/*

EXPOSE 45454/tcp

LABEL "org.opencontainers.image.authors"="hojmark"
LABEL "org.opencontainers.image.description"="Monitor network drift against your declared state"
LABEL "org.opencontainers.image.licenses"="AGPL-3.0"
LABEL "org.opencontainers.image.source"="https://github.com/hojmark/drift"
LABEL "org.opencontainers.image.title"="Drift"
LABEL "org.opencontainers.image.url"="https://docker.io/hojmark/drift"
LABEL "org.opencontainers.image.vendor"="hojmark"

# Override fedora-minimal labels (all OCI non-standard)
LABEL "io.buildah.version"=""
LABEL "license"=""
LABEL "name"=""
LABEL "org.opencontainers.image.license"=""
LABEL "org.opencontainers.image.name"=""
LABEL "vendor"=""
LABEL "version"=""

WORKDIR /app
COPY ./publish/linux-x64/drift .
ENTRYPOINT ["./drift"]