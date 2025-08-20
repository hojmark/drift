# Drift CLI Developer README

> [!TIP]
> You are probably looking for the regular [README](./README.md)!

> [!IMPORTANT]
> The details below may describe functionality or concepts that aren’t yet available in released versions.  
> Consider it an outline of the app's intended direction.

## Terminology

- **Spec**  
  A declarative definition of the desired state of network resources.

- **Declared Resource**  
  Any network resource (device, service, subnet, etc.) defined in the spec.

- **Discovered Resource**  
  Any network resource (device, service, subnet, etc.) found on the actual network during scanning.

- **Drift**  
  The difference between the declared state (from the spec) and the discovered state (from the network).

- **Device**  
  A network resource representing a physical or virtual device, identified by IP addresses, MAC address, and/or
  hostname.

- **Service**  
  TBD

- **Subnet**  
  A network resource representing a segment of the network, defined using CIDR notation (e.g., `192.168.1.0/24`).

- **[Device ID](#device-id)**  
  One or more addresses (MAC, IPv4, IPv6, and/or hostname) that together serve as a unique identifier for a network
  device.

## Concepts

### Device ID

A **device ID** may consist of one or more addresses, such as MAC, IPv4, IPv6, and/or hostname.

A *discovered* device (on the network) will only match a *declared* device (in the spec) if the device ID matches.

In the spec, you can mark one or more addresses with `is_id: true` (or leave it unspecified, which will default to
`true`) to indicate which should contribute to the device ID. Addresses marked with `is_id: false` are treated as
metadata and do not affect how the device is identified or matched.

## Installation Options

### Manual

Download the binary from the [Releases page](https://github.com/hojmark/drift/releases) and move it to your preferred
location.

### Script (`install.sh`)

Use the installation script to download and install Drift to your workstation, server or as part of an automation
pipeline.

Arguments, options and environment variables for `install.sh`:

- **(no arguments)**  
  Installs the latest version of Drift. Upgrades the existing binary if already present.

- **`<tag>`** (argument)  
  Installs a specific version of Drift. Upgrades or downgrades if a binary is already present.  
  To find available tags, visit the [Releases page](https://github.com/hojmark/drift/releases).
  Example tag: `v1.0.0-alpha.42`

- **`--verbose`** (option)  
  Enables verbose output during installation.

- **`DRIFT_INSTALL_DIR`** (environment variable)  
  By default, the script puts the `drift` binary into `/usr/local/bin`. Set this variable to change the installation
  directory.

### Container Image

_Coming soon_

### Package Manager

_Coming soon_

| Package Manager | Format              | Distribution           |
|-----------------|---------------------|------------------------|
| `apt`           | `.deb`              | APT repo?, direct .deb |
| `dnf`           | `.rpm`              | Copr, direct .rpm      |
| `pacman`        | PKGBUILD / `.pkg.*` | AUR, direct .pkg.*     |

## Spec

A Drift spec is a declarative definition of the desired state of network resources.

```yaml
TODO
```
