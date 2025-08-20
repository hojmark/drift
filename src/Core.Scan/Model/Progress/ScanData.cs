using Drift.Domain;
using Drift.Domain.Device.Addresses;

namespace Drift.Core.Scan.Model.Progress;

// Root scan data
public class NetworkScanData {
  public required List<CidrBlock> Subnets { get; init; }
  //public required ScanConfiguration Configuration { get; init; }
  public int TotalSubnets => Subnets.Count;
  public DateTime StartTime { get; init; }
}

// Subnet-level data
public class SubnetScanData {
  public required CidrBlock Subnet { get; init; }
  public required List<IpV4Address> DiscoveredIps { get; init; } = [];
  public required ScanPhase CurrentPhase { get; init; }
}

// Device-level data
public class DeviceScanData {
  public required string IpAddress { get; init; }
  public string? Hostname { get; init; }
  public string? MacAddress { get; init; }
  public required List<int> OpenPorts { get; init; } = [];
  public required List<int> PortsToScan { get; init; } = [];
  public int? CurrentPort { get; init; }
  
  public int TotalPortsToScan => PortsToScan.Count;
  public int PortsScanned { get; init; }
  public double PortScanProgress => TotalPortsToScan > 0 ? (double)PortsScanned / TotalPortsToScan : 0;
}


public enum ScanPhase { Discovery, DeviceEnumeration, PortScanning, ServiceDetection }

// Simplified subnet discovery data
public class SubnetDiscoveryData {
  public required List<SubnetSource> Sources { get; init; } = [];
  public required List<CidrBlock> FinalSubnets { get; init; } = [];
  public string? CurrentActivity { get; init; }
  
  public int TotalSources => Sources.Count;
  public int CompletedSources => Sources.Count(s => s.IsCompleted);
  public int TotalSubnetsFound => FinalSubnets.Count;
  //public int TotalAddresses => FinalSubnets.Sum(s => IpNetworkUtils.GetIpRangeCount(s));
}

public class SubnetSource {
  public required string Name { get; init; }
  public required string Type { get; init; }
  public required List<CidrBlock> Subnets { get; init; } = [];
  public required SourceStatus Status { get; init; }
  public string? ErrorMessage { get; init; }
  
  public bool IsCompleted => Status == SourceStatus.Completed || Status == SourceStatus.Failed || Status == SourceStatus.Skipped;
  public int SubnetCount => Subnets.Count;
}

public enum SourceStatus {
  NotStarted,
  InProgress, 
  Completed,
  Failed,
  Skipped
}
