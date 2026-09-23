using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;

namespace Drift.Cli.Tests.Utils.Testing;

/// <summary>
/// Helper for querying scan result data in tests.
/// Provides query methods that return data - tests should make assertions using standard Assert.That().
/// </summary>
public static class ScanResultHelper {
  /// <summary>
  /// Gets all discovered devices from all subnets in the scan result.
  /// </summary>
  public static List<DiscoveredDevice> GetAllDevices( NetworkScanResult result ) {
    return result.Subnets
      .SelectMany( s => s.DiscoveredDevices )
      .ToList();
  }

  /// <summary>
  /// Gets all discovered devices that have a specific IP address.
  /// </summary>
  public static List<DiscoveredDevice> GetDevicesWithIp( NetworkScanResult result, IpV4Address ipAddress ) {
    return GetAllDevices( result )
      .Where( d => d.Addresses.OfType<IpV4Address>().Any( ip => ip.Equals( ipAddress ) ) )
      .ToList();
  }

  /// <summary>
  /// Gets all discovered devices that have a specific MAC address.
  /// </summary>
  public static List<DiscoveredDevice> GetDevicesWithMac( NetworkScanResult result, MacAddress macAddress ) {
    return GetAllDevices( result )
      .Where( d => d.Addresses.OfType<MacAddress>().Any( mac => mac.Equals( macAddress ) ) )
      .ToList();
  }

  /// <summary>
  /// Gets all unique IP addresses discovered across all subnets.
  /// </summary>
  public static List<IpV4Address> GetAllIpAddresses( NetworkScanResult result ) {
    return GetAllDevices( result )
      .SelectMany( d => d.Addresses.OfType<IpV4Address>() )
      .Distinct()
      .ToList();
  }

  /// <summary>
  /// Gets all unique MAC addresses discovered across all subnets.
  /// </summary>
  public static List<MacAddress> GetAllMacAddresses( NetworkScanResult result ) {
    return GetAllDevices( result )
      .SelectMany( d => d.Addresses.OfType<MacAddress>() )
      .Distinct()
      .ToList();
  }

  /// <summary>
  /// Gets all subnets that were scanned (from the result).
  /// </summary>
  public static List<CidrBlock> GetScannedSubnets( NetworkScanResult result ) {
    return result.Subnets
      .Select( s => s.CidrBlock )
      .ToList();
  }

  /// <summary>
  /// Gets the subnet scan result for a specific CIDR block.
  /// Returns null if the subnet was not scanned.
  /// </summary>
  public static SubnetScanResult? GetSubnetResult( NetworkScanResult result, CidrBlock cidr ) {
    return result.Subnets.FirstOrDefault( s => s.CidrBlock.Equals( cidr ) );
  }

  /// <summary>
  /// Gets all subnets with a specific scan status.
  /// </summary>
  public static List<SubnetScanResult> GetSubnetsByStatus(
    NetworkScanResult result,
    ScanResultStatus status
  ) {
    return result.Subnets
      .Where( s => s.Status == status )
      .ToList();
  }

  /// <summary>
  /// Gets the total number of devices discovered across all subnets.
  /// </summary>
  public static int GetTotalDeviceCount( NetworkScanResult result ) {
    return GetAllDevices( result ).Count;
  }

  /// <summary>
  /// Gets the total number of unique IP addresses discovered.
  /// </summary>
  public static int GetUniqueIpCount( NetworkScanResult result ) {
    return GetAllIpAddresses( result ).Count;
  }

  /// <summary>
  /// Gets the total number of unique MAC addresses discovered.
  /// </summary>
  public static int GetUniqueMacCount( NetworkScanResult result ) {
    return GetAllMacAddresses( result ).Count;
  }

  /// <summary>
  /// Checks if a specific IP address was discovered in the scan.
  /// </summary>
  public static bool HasIpAddress( NetworkScanResult result, IpV4Address ipAddress ) {
    return GetDevicesWithIp( result, ipAddress ).Any();
  }

  /// <summary>
  /// Checks if a specific MAC address was discovered in the scan.
  /// </summary>
  public static bool HasMacAddress( NetworkScanResult result, MacAddress macAddress ) {
    return GetDevicesWithMac( result, macAddress ).Any();
  }

  /// <summary>
  /// Checks if a specific subnet was scanned.
  /// </summary>
  public static bool HasSubnet( NetworkScanResult result, CidrBlock cidr ) {
    return GetSubnetResult( result, cidr ) != null;
  }

  /// <summary>
  /// Gets devices discovered in a specific subnet.
  /// Returns empty list if the subnet was not scanned.
  /// </summary>
  public static List<DiscoveredDevice> GetDevicesInSubnet( NetworkScanResult result, CidrBlock cidr ) {
    var subnetResult = GetSubnetResult( result, cidr );
    return subnetResult?.DiscoveredDevices.ToList() ?? [];
  }

  /// <summary>
  /// Gets all subnets that had failures during scanning.
  /// </summary>
  public static List<CidrBlock> GetFailedSubnets( NetworkScanResult result ) {
    return GetSubnetsByStatus( result, ScanResultStatus.Error )
      .Select( s => s.CidrBlock )
      .ToList();
  }

  /// <summary>
  /// Gets all subnets that were scanned successfully.
  /// </summary>
  public static List<CidrBlock> GetSuccessfulSubnets( NetworkScanResult result ) {
    return GetSubnetsByStatus( result, ScanResultStatus.Success )
      .Select( s => s.CidrBlock )
      .ToList();
  }
}