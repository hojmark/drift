# Containerlab Integration Testing

This directory contains containerlab topologies for testing Drift's distributed network scanning capabilities.

## Prerequisites

- [Containerlab](https://containerlab.dev/) installed
- Docker with sufficient resources (at least 4GB RAM, 2 CPUs)
- Drift Docker image: `localhost:5000/drift:dev`

## Quick Start

The easiest way to run containerlab integration tests is via Nuke:

```bash
# Run all tests including containerlab integration
dotnet nuke Test

# Run only containerlab tests
dotnet nuke TestContainerlab --skip test

# Run a single topology for debugging
dotnet nuke TestContainerlab --skip test --clab-topology simple-test

# Keep containers running after tests (for debugging)
dotnet nuke TestContainerlab --skip test --keep-clab-running
```

## Topologies

### `simple-test.clab.yaml`

Minimal topology: 1 agent, 1 CLI, 1 target on the management network.

```
     CLI                 Agent1               Target1
(172.20.20.x)        (172.20.20.x)         (172.20.20.x)
       |                    |                     |
       +--------------------+---------------------+
                       172.20.20.0/24
```

**Assertions:**
- `172.20.20.0/24` is scanned
- 2/2 scan operations successful (local + 1 agent)
- Scan completes successfully

### `cooperation-test.clab.yaml`

Multi-agent cooperation topology: 3 agents, 1 CLI, 5 targets on a flat management network.
Tests multi-agent coordination and result merging.

```
CLI + Agent1 + Agent2 + Agent3 + Target1..5
              |
         172.20.20.0/24
```

**Assertions:**
- `172.20.20.0/24` is scanned
- 4/4 scan operations successful (local + 3 agents)
- Scan completes successfully

## Agent Identity

Agents in these topologies use the `--id` flag to set a fixed, predictable agent ID:

```yaml
agent1:
  kind: linux
  image: localhost:5000/drift:dev
  cmd: agent start --adoptable --port 5000 --id agentid_test1
```

The `--id` flag is hidden from the help output and logs a warning when used — it is only for testing.

In production, agents generate and persist their own ID at `/root/.config/drift/agent/agent-identity.json`.

## Nuke Target Parameters

| Parameter | Description |
|---|---|
| `--clab-topology <name>` | Run only the named topology (e.g. `simple-test`). Runs all if omitted. |
| `--skip-clab-deploy` | Skip deployment — useful when topology is already running |
| `--keep-clab-running` | Keep containers running after tests for debugging |

## Troubleshooting

**Deploy fails with "Link not found"** — This is a known issue with rootless Podman + pasta networking. The Nuke target works around it by pre-creating the `clab` management network before deploying. If you are deploying manually, run:
```bash
docker network rm clab 2>/dev/null; docker network create --subnet 172.20.20.0/24 --ipv6 --subnet 3fff:172:20:20::/64 clab
containerlab deploy --topo simple-test.clab.yaml
```

**Agents not starting** — Check container logs:
```bash
docker logs clab-drift-simple-test-agent1
docker logs clab-drift-cooperation-test-agent1
```

**Cannot reach agents** — Verify containers are on the management network:
```bash
docker network inspect clab
```

**Connection refused on first scan attempt** — Normal. The agent starts slowly and the client retries automatically.
