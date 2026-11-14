# Containerlab integration testing

This directory contains [Containerlab](https://containerlab.dev/) topologies for testing Drift's distributed network scanning capabilities.

## Prerequisites

- Linux
- [Containerlab](https://containerlab.dev/) installed
- Docker, or Podman with Docker CLI shim
- Drift Docker image: `localhost:5000/drift:dev`

## Quick start

```bash
# Run ALL tests including Containerlab integration tests
dotnet nuke Test

# Run only containerlab tests
dotnet nuke TestE2E_Clab

# Run a single topology for debugging
dotnet nuke TestE2E_Clab --clab-topology simple-test

# Keep containers running after tests (for debugging)
dotnet nuke TestE2E_Clab --keep-clab-running
```

The target deploys and destroys each topology automatically. Use `--skip-clab-deploy` to run against an already deployed topology.

## NUKE target parameters

| Parameter | Description |
| --- | --- |
| `--clab-topology <name>` | Run only the named topology (e.g. `simple-test`). Runs all if omitted. |
| `--skip-clab-deploy` | Skip deployment — useful when the topology is already running |
| `--keep-clab-running` | Keep containers running after tests for debugging |

## Troubleshooting

**Deploy fails with "Link not found"** — This is a known issue with rootless Podman + pasta networking. The NUKE target works around it by pre-creating the `clab` management network before deploying. If you are deploying manually, run:

```bash
docker network rm clab 2>/dev/null; docker network create --subnet 172.20.20.0/24 --ipv6 --subnet 3fff:172:20:20::/64 clab
containerlab deploy --topo simple-test.clab.yaml
```

**Agents not starting** — Check container logs:

```bash
docker logs clab-drift-simple-test-agent1
docker logs clab-drift-cooperation-test-agent1
docker logs clab-drift-subnet-isolation-test-agent1
```

**Cannot reach agents** — Verify containers are on same network:

```bash
docker network inspect clab
```

**Connection refused on first scan attempt** — Normal. The agent starts slowly and the client retries automatically.
