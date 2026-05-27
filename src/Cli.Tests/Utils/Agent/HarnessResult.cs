using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils.Testing;
using Drift.Domain;
using Drift.Domain.Scan;

namespace Drift.Cli.Tests.Utils.Agent;

/// <summary>
/// Result from running a scan with the agent test harness.
///
/// RECOMMENDED USAGE:
/// 1. Assert on exit codes: Assert.That(result.ScanExitCode, Is.EqualTo(ExitCodes.Success))
/// 2. Use Verify for output: await Verify(result.CombinedOutput)
/// 3. Query results if needed: result.Results.GetDevicesWithIp(...)
///
/// Only use result.Logs when you need to programmatically query specific log entries.
/// For most tests, Verify on CombinedOutput is easier to work with.
/// </summary>
public sealed class HarnessResult {
  /// <summary>
  /// Gets the exit code from the scan command.
  /// </summary>
  public required int ScanExitCode {
    get;
    init;
  }

  /// <summary>
  /// Gets the standard output from the scan command.
  /// </summary>
  public required string ScanOutput {
    get;
    init;
  }

  /// <summary>
  /// Gets the standard error from the scan command.
  /// </summary>
  public required string ScanError {
    get;
    init;
  }

  /// <summary>
  /// Gets the combined output (stdout + stderr).
  /// </summary>
  public string CombinedOutput => ScanOutput + ScanError;

  /// <summary>
  /// Gets the parsed scan result (if scan was successful).
  /// </summary>
  public NetworkScanResult? ScanResult {
    get;
    init;
  }

  /// <summary>
  /// Gets the exit codes from each agent, keyed by agent ID.
  /// </summary>
  public required Dictionary<AgentId, int> AgentExitCodes {
    get;
    init;
  }

  /// <summary>
  /// Gets all agent IDs from the result.
  /// </summary>
  public IEnumerable<AgentId> AgentIds => AgentExitCodes.Keys;

  /// <summary>
  /// Gets a value indicating whether the scan completed successfully.
  /// </summary>
  public bool IsSuccess => ScanExitCode == ExitCodes.Success;

  /// <summary>
  /// Gets the query methods for scan results (convenience accessors to ScanResultHelper).
  /// </summary>
  public ScanResultQuery Results => new(ScanResult);
}

/// <summary>
/// Convenience wrapper for querying scan results.
/// Provides direct access to ScanResultHelper methods without needing to pass the result each time.
/// </summary>
public sealed class ScanResultQuery( NetworkScanResult? result ) {
  private readonly NetworkScanResult _result = result ?? throw new InvalidOperationException(
    "Cannot query results - scan did not produce a result"
  );

  public List<Drift.Domain.Device.Discovered.DiscoveredDevice> GetAllDevices() =>
    ScanResultHelper.GetAllDevices( _result );

  public List<Drift.Domain.Device.Discovered.DiscoveredDevice> GetDevicesWithIp(
    Drift.Domain.Device.Addresses.IpV4Address ipAddress ) =>
    ScanResultHelper.GetDevicesWithIp( _result, ipAddress );

  public List<Drift.Domain.Device.Discovered.DiscoveredDevice> GetDevicesWithMac(
    Drift.Domain.Device.Addresses.MacAddress macAddress ) =>
    ScanResultHelper.GetDevicesWithMac( _result, macAddress );

  public List<Drift.Domain.Device.Addresses.IpV4Address> GetAllIpAddresses() =>
    ScanResultHelper.GetAllIpAddresses( _result );

  public List<Drift.Domain.Device.Addresses.MacAddress> GetAllMacAddresses() =>
    ScanResultHelper.GetAllMacAddresses( _result );

  public List<Drift.Domain.CidrBlock> GetScannedSubnets() =>
    ScanResultHelper.GetScannedSubnets( _result );

  public SubnetScanResult? GetSubnetResult( Drift.Domain.CidrBlock cidr ) =>
    ScanResultHelper.GetSubnetResult( _result, cidr );

  public List<SubnetScanResult> GetSubnetsByStatus( ScanResultStatus status ) =>
    ScanResultHelper.GetSubnetsByStatus( _result, status );

  public int GetTotalDeviceCount() =>
    ScanResultHelper.GetTotalDeviceCount( _result );

  public int GetUniqueIpCount() =>
    ScanResultHelper.GetUniqueIpCount( _result );

  public int GetUniqueMacCount() =>
    ScanResultHelper.GetUniqueMacCount( _result );

  public bool HasIpAddress( Drift.Domain.Device.Addresses.IpV4Address ipAddress ) =>
    ScanResultHelper.HasIpAddress( _result, ipAddress );

  public bool HasMacAddress( Drift.Domain.Device.Addresses.MacAddress macAddress ) =>
    ScanResultHelper.HasMacAddress( _result, macAddress );

  public bool HasSubnet( Drift.Domain.CidrBlock cidr ) =>
    ScanResultHelper.HasSubnet( _result, cidr );

  public List<Drift.Domain.Device.Discovered.DiscoveredDevice> GetDevicesInSubnet( Drift.Domain.CidrBlock cidr ) =>
    ScanResultHelper.GetDevicesInSubnet( _result, cidr );

  public List<Drift.Domain.CidrBlock> GetFailedSubnets() =>
    ScanResultHelper.GetFailedSubnets( _result );

  public List<Drift.Domain.CidrBlock> GetSuccessfulSubnets() =>
    ScanResultHelper.GetSuccessfulSubnets( _result );
}