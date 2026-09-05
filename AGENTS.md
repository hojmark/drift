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
dotnet nuke PublishBinaries    # Self-contained binary for the current platform
dotnet nuke BuildContainerImage
```

Run a single test class or filter by name using standard `dotnet test` filters:

```sh
dotnet test src/Domain.Tests --filter "FullyQualifiedName~MyTest"
```

## Architecture

### Source layout (`src/`)

The solution is split into focused projects. The main ones:

| Project | Role |
|---|---|
| `Cli` | Entry point; commands: `init`, `scan`, `agent start`; AOT-compiled |
| `Cli.Abstractions` | Shared CLI constants: exit codes, env var names, port numbers, file names |
| `Cli.Settings` | User settings file (`~/.config/drift/settings.json`) |
| `Domain` | Core value types: `Network`, `Device`, `Inventory`, `CidrBlock`, `Port`, `AgentId` |
| `Spec` | YAML spec parsing and validation into declared-state domain types |
| `Scanning` | Network discovery: ARP, ping, port scanning |
| `Diff` | Compares declared spec state vs. discovered scan state to produce a drift report |
| `Networking.Grpc` | Generated gRPC/protobuf contracts for the messaging transport |
| `Networking.Core.Abstractions` | Interfaces for message streams, handlers, and client factories |
| `Networking.Core` | Message stream/manager implementation built on gRPC |
| `Networking.Client` | Default client factory for opening outbound messaging connections |
| `Networking.Server` | Hosts the inbound gRPC service for messaging endpoints |
| `Messaging.Protocol.Agent` | Agent request/response message contracts (e.g. scan, subnets) |
| `Messaging.Client` | Typed agent client built on top of `Networking.Client`/`Networking.Core` |
| `Agent.Host` | Hosts an agent's messaging/gRPC endpoint (Kestrel/ASP.NET Core) |
| `Coordinator.Host` | Coordinator-side host counterpart to `Agent.Host` (work in progress) |
| `Common` | Shared cross-cutting helpers: IO, logging, network utilities, embedded resources |
| `Common.Schemas` | Shared JSON Schema generation helpers (e.g. lowercase enum naming) |
| `Serialization` | Cross-module serialization helpers |
| `TestUtilities` | Shared test helpers (loggers, Verify/snapshot settings) used by `*.Tests` projects |
| `ArchTests` | ArchUnitNET tests enforcing dependency rules and naming conventions |

Schema generators live in `Spec.SchemaGenerator.Cli` and `Cli.Settings.SchemaGenerator.Cli` — they produce JSON Schema from C# types.

`Networking.*` and `Messaging.*` implement the role-agnostic transport layer (see naming rule below); `Agent.Host` and `Coordinator.Host` build role-specific hosting on top of it.

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
- **`Networking.*` are role-agnostic**: No "Agent", "Peer", "Coordinator", or "Server" in type names, property names, parameter names, method names, or log strings inside `Networking.*`. These assemblies implement the transport layer only (streams, messages, connections). Role-specific concerns belong in `Agent.*`, `Coordinator.*`, or `Cli.*`.

## Testing

- **Unit tests**: `*.Tests` projects using NUnit 4 and NSubstitute for mocking.
- **E2E tests**: `Cli.E2ETests.*` projects (`General` install scripts and schemas, `Binary` against the published binary, `Container` against the container image).
- **Containerlab tests**: driven directly by the NUKE build (`build/NukeBuild.TestContainerlab.cs`, target `TestE2E_Clab`) against multi-node topologies — not a `Cli.E2ETests.*` project. Requires Containerlab installed and uses topology files in `containerlab/`.
- **Architecture tests**: `ArchTests` project validates project dependency graph and naming rules.

## Terminology (from domain model)

- **Spec**: declarative YAML definition of desired network state
- **Declared resource**: a device/subnet defined in the spec
- **Discovered resource**: a device/subnet found by scanning
- **Drift**: difference between declared and discovered state
- **Device ID**: one or more addresses (MAC, IPv4, IPv6, hostname) that uniquely identify a device; spec addresses with `is_id: false` are metadata only
- **Agent**: a Drift instance in agent mode that reports scan results to peers
