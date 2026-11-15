using System.Collections.Immutable;
using System.Net;
using Drift.Common;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;

namespace Drift.Scanning.Scanners;

internal static class FpingScanResultMapper {
  internal static SubnetScanResult Create(
    CidrBlock cidr,
    IReadOnlyCollection<IPAddress> ipRange,
    IReadOnlyCollection<DiscoveredDevice> discoveredDevices,
    ToolResult toolResult,
    DateTime startedAt,
    DateTime endedAt
  ) {
    var status = toolResult.Cancelled
      ? ScanResultStatus.Canceled
      // Exit code 1 means the scan completed but some hosts did not reply (very expected)
      : toolResult.ExitCode is 0 or 1
        ? ScanResultStatus.Success
        : ScanResultStatus.Error;

    string? errorMessage = null;
    if ( status == ScanResultStatus.Error ) {
      var errorDetails = string.IsNullOrWhiteSpace( toolResult.ErrOut )
        ? string.Empty
        : $" {toolResult.ErrOut.Trim()}";
      errorMessage = $"fping failed with exit code {toolResult.ExitCode}.{errorDetails}";
    }

    // TODO not correct. need probe result for each device to determine this
    var discoveryAttempts = discoveredDevices
      .SelectMany( device => device.Addresses.OfType<IpV4Address>() )
      .ToImmutableHashSet();

    if ( status == ScanResultStatus.Success ) {
      discoveryAttempts = ipRange.Select( ip => new IpV4Address( ip ) ).ToImmutableHashSet();
    }

    return new SubnetScanResult {
      Metadata = new Metadata { StartedAt = startedAt, EndedAt = endedAt },
      Status = status,
      CidrBlock = cidr,
      DiscoveredDevices = discoveredDevices,
      DiscoveryAttempts = discoveryAttempts,
      ErrorMessage = errorMessage
    };
  }
}