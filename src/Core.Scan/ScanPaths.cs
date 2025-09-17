using Drift.Domain;
using Drift.Domain.Device.Discovered;
using Drift.Domain.NeoProgress;
using Path = Drift.Domain.NeoProgress.Path;

namespace Drift.Core.Scan;

public static class ScanPaths {
  public static readonly Path Root = "Root";

  public static class SubnetDiscovery {
    public static readonly Path Self = Root / "Subnet discovery";
    public static readonly Path FromSpec = Self / "From spec";
    public static readonly Path FromInterfaces = Self / "From interfaces";

    public static class ContextKeys {
      public static readonly ContextKey<List<CidrBlock>> InterfaceSubnets = new("InterfaceSubnets");
      public static readonly ContextKey<List<CidrBlock>> SpecSubnets = new("SpecSubnets");
      public static readonly ContextKey<List<CidrBlock>> FinalSubnets = new("FinalSubnets");
      public static readonly ContextKey<int> DuplicatesRemoved = new("DuplicatesRemoved");
      public static readonly ContextKey<int> TotalFound = new("TotalFound");
    }
  }

  public static class DeviceDiscovery {
    public static readonly Path Self = Root / "Device discovery";
    public static readonly Path PingScanning = Self / "Ping scan";
    public static readonly Path ArpResolution = Self / "ARP resolution";

    public static class ContextKeys {
      public static readonly ContextKey<DiscoveredDevice> DeviceFound = new("DeviceFound");
      public static readonly ContextKey<List<DiscoveredDevice>> AllDevices = new("AllDevices");
      public static readonly ContextKey<int> DeviceCount = new("DeviceCount");
    }
  }
}