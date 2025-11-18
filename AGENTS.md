# Drift CLI - Agent guide (required for )

> REQUIRED: agents must read this file before contributing.

## Project overview

Drift is a CLI tool for detecting when real network state differs from a declared desired state.

Features include:

- network device discovery by scanning a real network (IPv4, MAC, hostname)
- drift detection (using a network spec YAML file as the baseline)
- interactive CLI (only some commands, currently `init` and `scan`)

Status: Alpha — APIs, schemas, and file formats may change frequently. The `v1-preview` version of spec and settings schemas are ok to alter until the project reaches beta.

### Basic usage
Note: `drift-dev` is an alias that points to the development CLI binary (rebuild from source by re-building the solution). When installed on a user's system `drift` is used. You should never use `drift`.
```bash
# List available commands
drift-dev --help

# Create network spec (is interactive when no parameters are provided)
drift-dev init

# Scan and detect drift (use -i for interactive mode)
drift-dev scan
```

## Architecture

### Technology stack
- **Platform**: .NET 10, C#, NuGet CPM
- **Build system**: [NUKE](https://nuke.build/)
- **Packaging**: Native binary (.NET NativeAOT) and container image
- **Testing**: NUnit (primarily), TUnit
- **Continuous Integration**: GitHub Actions

### Project structure

```
src/
├── Cli/                              # Main CLI application
├── Cli.Settings/                     # User settings management
├── Cli.Settings.SchemaGenerator.Cli/ # JSON schema generator for settings
├── Domain/                           # Core domain models
├── Spec/                             # Network spec
├── Spec.SchemaGenerator.Cli/         # JSON schema generator for network specs
├── Scanning/                         # Network scanning functionality
├── Diff/                             # Generic comparison logic (especially used for drift detection)
├── Serialization/                    # JSON serialization
├── Common/                           # Shared utilities (ideally this should be eliminated or as small as possible)
├── TestUtilities/                    # Testing helpers
├─ *Tests/                            # Unit, E2E and architecture test projects
... (more)
```

### Command to file mapping examples
```
drift-dev init -> src/Cli/Commands/init/InitCommand.cs
drift-dev init -> src/Cli/Commands/init/ScanCommand.cs
```

## Development guidelines

### Important!
Code changes must always compile and all tests must always pass!

### Building
```bash
# Build solution
dotnet build Drift.sln

# List all available targets
dotnet nuke --help

# Run all tests (unit, E2E, architecture)
dotnet nuke test

# Check build warnings
dotnet nuke build+checkbuildwarnings # AFTER the command has run, find warnings in: build.binlog-warnings-only.log

# Check publish warnings
dotnet nuke publishbinaries+checkpublishbinarieswarnings # AFTER the command has run, find warnings in: publish.binlog-warnings-only.log
```

### Code standards
All projects have the following MSBuild properties set (they are specified in `Directory.Build.props` which gets imported by all projects):
```xml
<AssemblyName>Drift.$(MSBuildProjectName)</AssemblyName>
<RootNamespace>Drift.$(MSBuildProjectName)</RootNamespace>
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

Do not use Console.WriteLine or similar for logging or output. Use `IOutputManager` in the `Cli` project and `ILogger` in other projects.

### Testing
- Unit tests: `*.Tests` projects
- E2E tests: `Cli.E2ETests`
- Architecture tests: `ArchTests`
- Test utilities: `TestUtilities`

### Common Tasks

1. **Adding a new command**: Work in `Cli` project
2. **Network scanning logic**: Modify `Scanning` project
3. **Drift detection**: Update `Diff` project
4. **Domain models**: Change `Domain` project
5. **Spec format changes**: Update `Spec` + regenerate schemas
6. **Settings format changes**: Update `Cli.Settings` + regenerate schemas
7. **Fix build warnings**: run `dotnet nuke publishbinaries` and find warnings in build.binlog-warnings-only.log (build warnings) and publish.binlog-warnings-only.log (publish warnings). Do not fix nuget advisories unless explicitly instructed to do so. A build warning may also be ignored if it can be justified.

## Configuration files

### Network specs
- **Location**: User-defined (typically `drift-spec.yaml`)
- **Format**: YAML
- **Schema**: Available at `src/Spec/embedded_resources/schemas/drift-spec-v1-preview.schema.json`
- **Purpose**: Define desired network state

### User settings
- **Location**: `~/.config/drift/settings.json`
- **Format**: JSON
- **Schema**: Available at `src/Cli.Settings/embedded_resources/schemas/drift-settings-v1-preview.schema.json`
- **Purpose**: User preferences and configuration

## File locations
- **Source code**: `src/`
- **Build script**: `build/NukeBuild.cs` (other partial classes in `NukeBuild.*.cs`)
- **Build output**: `artifacts/` (what gets released), `publish/` (native binaries), container image at `docker.io/hojmark/drift`
- **Schemas**: embedded in respective projects under `embedded_resources/schemas/`
- **Documentation**: `README.md`, `README_dev.md`, `SECURITY.md`
