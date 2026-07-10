# AGENTS.md

This file provides guidance to AI agents when working with code in this repository.

## Project Overview

Drift is a .NET 10 CLI tool for network drift detection — it compares a declarative YAML spec (desired network state) against live network scanning results and reports differences. It supports distributed scanning via agents communicating over gRPC.

## Build System

The build uses [NUKE](https://nuke.build/). Entry point is `dotnet nuke`.

Common targets:

```sh
dotnet nuke Build              # Restore + compile
dotnet nuke TestUnit           # Unit tests only (fast)
dotnet nuke Test               # All tests (unit + E2E)
dotnet nuke TestE2E            # E2E tests (General, Binary, Container image, Container network topologies using Containerlab)
dotnet nuke PublishBinaries    # Self-contained binary (
dotnet nuke BuildContainerImage
```

Run a single test class or filter by name using standard `dotnet test` filters:

```sh
dotnet test src/Domain.Tests --filter "FullyQualifiedName~MyTest"
```

## Architecture

### Source layout (`src/`)

The solution is split into focused projects. The main ones:

| Project | Role                                                                                     |
|---|------------------------------------------------------------------------------------------|
| `Cli` | Entry point; commands: `init`, `scan`, `agent start`; AOT-compiled                       |
| `Cli.Abstractions` | Shared CLI constants: exit codes, env var names, port numbers, file names                |
| `Cli.Settings` | User settings file (`~/.config/drift/settings.json`)                                     |
| `Domain` | Core value types: `Network`, `Device`, `Inventory`, `CidrBlock`, `Port`, `Scan`, `AgentId` |
| `Spec` | YAML spec parsing and validation (YamlDotNet + JsonSchema.Net)                           |
| `Scanning` | Network discovery: ARP, ping, port scanning with rate limiting                           |
| `Diff` | Compares declared spec state vs. discovered scan state to produce drift report           |
| `Networking.PeerStreaming.*` | Abstract P2P streaming protocol + gRPC implementation (`peer.proto`)                     |
| `Networking.Cluster` | Multi-agent coordination                                                                 |
| `Agent.Hosting` | Agent runtime (`AgentHost`, `Identity`)                                                  |
| `Agent.PeerProtocol` | Agent-specific peer messaging                                                            |
| `Serialization` | Cross-module serialization helpers                                                       |
| `ArchTests` | ArchUnitNET tests enforcing dependency rules and naming conventions                      |

Schema generators live in `Spec.SchemaGenerator.Cli` and `Cli.Settings.SchemaGenerator.Cli` — they produce JSON Schema from C# types.

### Data flow

```
YAML spec → Spec (parse/validate) → Domain types (declared state)
                                          ↓
Network → Scanning → Domain types (discovered state)
                                          ↓
                              Diff → Drift report → Cli (render)
```

Agents (remote Drift instances) report discovered state back to the coordinator over gRPC, extending scan coverage across subnets.

### Key conventions

- **Central package management**: all NuGet versions in `Directory.Packages.props`; do not add `Version=` attributes to `<PackageReference>` in individual project files.
- **Shared project defaults**: `Directory.Build.props` applies nullable refs, implicit usings, and logging config to all projects.
- **InternalsVisibleTo**: test projects access internal members for white-box testing; this is intentional.
- **Snapshot testing**: `Verify.NUnit` is used for golden file comparisons. Run tests to regenerate snapshots when output changes; committed `.verified.*` files are the source of truth.
- **AOT**: `Cli` is published with `PublishAot=true`. Avoid reflection-heavy patterns in the CLI project; use source generators instead.
- **Embedded resources**: schemas, default specs, and scripts are embedded in project assemblies under `embedded_resources/`.

## Testing

- **Unit tests**: `*.Tests` projects using NUnit 4 and NSubstitute for mocking.
- **E2E tests**: `Cli.E2ETests.*` projects — General (Testcontainers), Binary (published binary), Container (Docker image), Containerlab (real multi-node topologies).
- **Architecture tests**: `ArchTests` project validates project dependency graph and naming rules.

Containerlab tests require Containerlab installed and use topology files in `containerlab/`.

## Terminology (from domain model)

- **Spec**: declarative YAML definition of desired network state
- **Declared resource**: a device/subnet defined in the spec
- **Discovered resource**: a device/subnet found by scanning
- **Drift**: difference between declared and discovered state
- **Device ID**: one or more addresses (MAC, IPv4, IPv6, hostname) that uniquely identify a device; spec addresses with `is_id: false` are metadata only
- **Agent**: a Drift instance in agent mode that reports scan results to peers
