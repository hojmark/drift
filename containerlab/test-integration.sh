#!/bin/bash
# Integration test for distributed network scanning MVP
set -e

echo "=== Drift Distributed Scanning Integration Test ==="
echo

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test configuration
CLAB_TOPO="containerlab/distributed-scan-mvp.clab.yaml"
TEST_INVENTORY="containerlab/test-inventory.yaml"

function log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

function log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

function log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

function cleanup() {
    log_info "Cleaning up containerlab topology..."
    sudo containerlab destroy -t "$CLAB_TOPO" --cleanup 2>/dev/null || true
}

# Trap to ensure cleanup on exit
trap cleanup EXIT

log_info "Step 1: Deploy containerlab topology"
sudo containerlab deploy -t "$CLAB_TOPO"

log_info "Step 2: Wait for agents to start..."
sleep 10

log_info "Step 3: Check agent connectivity"
for port in 5001 5002 5003; do
    if curl -s -o /dev/null -w "%{http_code}" "http://localhost:$port/health" | grep -q "200"; then
        log_info "  Agent on port $port is healthy"
    else
        log_warn "  Agent on port $port is not responding"
    fi
done

log_info "Step 4: Check agent identity persistence"
log_info "  Checking if agent identity files were created..."
docker exec clab-drift-distributed-scan-mvp-agent1 ls -la ~/.config/drift/agent/ || log_warn "  Agent1 identity not found"
docker exec clab-drift-distributed-scan-mvp-agent2 ls -la ~/.config/drift/agent/ || log_warn "  Agent2 identity not found"
docker exec clab-drift-distributed-scan-mvp-agent3 ls -la ~/.config/drift/agent/ || log_warn "  Agent3 identity not found"

log_info "Step 5: Discover subnets from agents"
log_info "  This should discover:"
log_info "    - 192.168.10.0/24 from agent1 and agent3"
log_info "    - 192.168.20.0/24 from agent2 and agent3"
docker exec clab-drift-distributed-scan-mvp-cli drift scan discover --spec "$TEST_INVENTORY"

log_info "Step 6: Run distributed scan"
log_info "  Testing full network scan with:"
log_info "    - Source-based assignment"
log_info "    - Overlapping subnet handling (192.168.10.0/24 and 192.168.20.0/24 visible to agent3)"
log_info "    - Result merging"
docker exec clab-drift-distributed-scan-mvp-cli drift scan --spec "$TEST_INVENTORY" -o json > scan-results.json

log_info "Step 7: Verify scan results"
if [ -f scan-results.json ]; then
    log_info "  Scan completed. Results saved to scan-results.json"
    
    # Check for expected devices
    FOUND_A1=$(jq -r '.subnets[] | select(.cidrBlock == "192.168.10.0/24") | .discoveredDevices[] | select(.addresses[].value == "192.168.10.100")' scan-results.json)
    FOUND_A2=$(jq -r '.subnets[] | select(.cidrBlock == "192.168.10.0/24") | .discoveredDevices[] | select(.addresses[].value == "192.168.10.101")' scan-results.json)
    FOUND_B1=$(jq -r '.subnets[] | select(.cidrBlock == "192.168.20.0/24") | .discoveredDevices[] | select(.addresses[].value == "192.168.20.100")' scan-results.json)
    FOUND_B2=$(jq -r '.subnets[] | select(.cidrBlock == "192.168.20.0/24") | .discoveredDevices[] | select(.addresses[].value == "192.168.20.101")' scan-results.json)
    
    [ -n "$FOUND_A1" ] && log_info "  ✓ Found target-a1 (192.168.10.100)" || log_error "  ✗ Missing target-a1"
    [ -n "$FOUND_A2" ] && log_info "  ✓ Found target-a2 (192.168.10.101)" || log_error "  ✗ Missing target-a2"
    [ -n "$FOUND_B1" ] && log_info "  ✓ Found target-b1 (192.168.20.100)" || log_error "  ✗ Missing target-b1"
    [ -n "$FOUND_B2" ] && log_info "  ✓ Found target-b2 (192.168.20.101)" || log_error "  ✗ Missing target-b2"
else
    log_error "  Scan results not found!"
    exit 1
fi

log_info "Step 8: Test retry logic"
log_info "  Stopping agent2 to simulate failure..."
docker stop clab-drift-distributed-scan-mvp-agent2
sleep 2

log_info "  Running scan with failed agent (should retry and show warnings)..."
docker exec clab-drift-distributed-scan-mvp-cli drift scan --spec "$TEST_INVENTORY" -o json 2>&1 | tee scan-with-failure.log

if grep -q "WARNING" scan-with-failure.log || grep -q "partial results" scan-with-failure.log; then
    log_info "  ✓ Retry logic and warning messages working"
else
    log_warn "  Expected warning messages not found in output"
fi

log_info ""
log_info "=== Integration Test Complete ==="
log_info "Results:"
log_info "  - Scan results: scan-results.json"
log_info "  - Failure test log: scan-with-failure.log"
