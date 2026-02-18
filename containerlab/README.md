# Containerlab Integration Testing

This directory contains containerlab topologies and test scripts for validating the distributed network scanning MVP.

## Prerequisites

- [Containerlab](https://containerlab.dev/) installed
- Docker with sufficient resources (at least 4GB RAM, 2 CPUs)
- `jq` for JSON processing in test scripts
- Drift Docker image built: `hojmark/drift:latest`

## Quick Start

```bash
# Make test script executable
chmod +x containerlab/test-integration.sh

# Build Drift Docker image (if not already built)
docker build -t hojmark/drift:latest .

# Run integration tests
./containerlab/test-integration.sh
```

## Topology Overview

### `distributed-scan-mvp.clab.yaml`

This topology creates a realistic multi-segment network to test distributed scanning:

```
                    Management Network (10.0.0.0/24)
                            |
        +-------------------+-------------------+
        |                   |                   |
    CLI Node            Agent1              Agent2           Agent3
  (10.0.0.10)         (10.0.0.11)         (10.0.0.12)      (10.0.0.13)
                          |                   |             |       |
                     Segment A           Segment B    Segment A  Segment B
                  (192.168.10.0/24)   (192.168.20.0/24)
                          |                   |
                    +-----+-----+       +-----+-----+
                    |           |       |           |
                Target-A1   Target-A2  Target-B1  Target-B2
              (.10.100)   (.10.101)  (.20.100)  (.20.101)
```

**Key features:**
- **Network isolation**: CLI cannot directly reach segment networks
- **Multi-homed agent**: Agent3 can see both segments (tests overlapping subnets)
- **Source-based assignment**: Each agent scans subnets it can reach
- **Result merging**: Agent3 provides additional coverage for both segments

## What Gets Tested

1. **Agent Identity Persistence**
   - Agents generate and persist UUIDs on first start
   - Identity survives container restarts

2. **Distributed Subnet Discovery**
   - Agents report their visible subnets
   - CLI aggregates subnet information from all agents

3. **Delegated Scanning**
   - CLI delegates scanning to agents based on subnet visibility
   - Progress updates stream back to CLI in real-time

4. **Overlapping Subnet Handling**
   - Multiple agents scanning the same subnet from different positions
   - Results merged to show complete device list

5. **Retry Logic and Error Handling**
   - Automatic retry with exponential backoff on transient failures
   - Warning logs for failed agents
   - Partial results when some agents fail

6. **Result Aggregation**
   - Device deduplication across multiple scans
   - Metadata merging (earliest start, latest end)
   - Accurate statistics reporting

## Manual Testing

### Deploy the topology

```bash
sudo containerlab deploy -t containerlab/distributed-scan-mvp.clab.yaml
```

### Check agent health

```bash
# Should return 200 OK for healthy agents
curl http://localhost:5001/health  # agent1
curl http://localhost:5002/health  # agent2
curl http://localhost:5003/health  # agent3
```

### Verify agent identities

```bash
docker exec clab-drift-distributed-scan-mvp-agent1 cat ~/.config/drift/agent/agent-identity.json
docker exec clab-drift-distributed-scan-mvp-agent2 cat ~/.config/drift/agent/agent-identity.json
docker exec clab-drift-distributed-scan-mvp-agent3 cat ~/.config/drift/agent/agent-identity.json
```

### Discover subnets

```bash
docker exec clab-drift-distributed-scan-mvp-cli \
  drift scan discover --spec /path/to/test-inventory.yaml
```

Expected output:
- `192.168.10.0/24` visible to agent1 and agent3
- `192.168.20.0/24` visible to agent2 and agent3

### Run distributed scan

```bash
docker exec clab-drift-distributed-scan-mvp-cli \
  drift scan --spec /path/to/test-inventory.yaml -o json
```

Expected results:
- All 4 target devices discovered
- Logs showing scans delegated to appropriate agents
- Merged results for overlapping subnets

### Test failure scenarios

```bash
# Stop an agent
docker stop clab-drift-distributed-scan-mvp-agent2

# Run scan - should see retries and warnings
docker exec clab-drift-distributed-scan-mvp-cli \
  drift scan --spec /path/to/test-inventory.yaml

# Check logs for retry attempts and partial results warning
```

### Cleanup

```bash
sudo containerlab destroy -t containerlab/distributed-scan-mvp.clab.yaml --cleanup
```

## Troubleshooting

### Agents not starting
- Check Docker logs: `docker logs clab-drift-distributed-scan-mvp-agent1`
- Verify ports are available: `netstat -tlnp | grep -E '500[1-3]'`

### Cannot reach agents from CLI
- Verify management network connectivity
- Check agent endpoints in inventory file match containerlab node names

### Scans timing out
- Increase timeout in ClusterOptions (default: 30s for regular, 5min for streaming)
- Check network latency between CLI and agents

### Missing devices in results
- Verify target containers are running: `docker ps | grep target`
- Check IP addresses are configured: `docker exec <target> ip addr`
- Ensure agents can ping targets from their segment networks

## CI/CD Integration

To integrate these tests into CI:

```yaml
test-integration:
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v3
    
    - name: Install containerlab
      run: |
        sudo sh -c "$(curl -sL https://get.containerlab.dev)"
    
    - name: Build Drift image
      run: docker build -t hojmark/drift:latest .
    
    - name: Run integration tests
      run: |
        chmod +x containerlab/test-integration.sh
        ./containerlab/test-integration.sh
    
    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v3
      with:
        name: integration-test-results
        path: |
          scan-results.json
          scan-with-failure.log
```
