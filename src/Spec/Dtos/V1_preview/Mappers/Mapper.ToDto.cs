using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;

namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static partial class Mapper {
  internal const string VersionConstant = "v1-preview";

  public static DriftSpec ToDto( Inventory domain ) {
    return new DriftSpec {
      Version = VersionConstant,
      Network = Map( domain.Network ),
      Server = domain.Server == null ? null : Map( domain.Server ),
      Settings = domain.Settings == null ? null : Map( domain.Settings ),
      Agents = domain.Agents.Count == 0 ? null : domain.Agents.Select( Map ).ToList()
    };
  }

  private static Server Map( Domain.Server domain ) {
    return new Server { Address = domain.Address };
  }

  private static Agent Map( Domain.Agent domain ) {
    return new Agent { Id = domain.Id, Address = domain.Address, Policy = domain.Policy?.Select( Map ).ToList() };
  }

  private static Policy Map( Domain.Policy domain ) {
    return new Policy {
      To = domain.To,
      Expect = domain.Expect,
      Port = domain.Port,
      Protocol = domain.Protocol,
      Probe = domain.Probe,
      Fallback = domain.Fallback
    };
  }

  private static Settings Map( Domain.Settings domain ) {
    return new Settings {
      UnknownDevices = Map( domain.UnknownDevices ),
      UndeclaredConnections = Map( domain.UndeclaredConnections ),
      PingThrottling = domain.PingThrottling,
      ScanOnlyDeclaredSubnets = domain.ScanOnlyDeclaredSubnets
    };
  }

  private static UnknownDevicePolicy? Map( Domain.UnknownDevicePolicy? domain ) {
    return domain switch {
      null => null,
      Domain.UnknownDevicePolicy.Disallowed => UnknownDevicePolicy.Disallowed,
      Domain.UnknownDevicePolicy.Allowed => UnknownDevicePolicy.Allowed,
      _ => throw new ArgumentOutOfRangeException( nameof(domain), domain, null )
    };
  }

  private static UndeclaredConnectionsPolicy? Map( Domain.UndeclaredConnectionsPolicy? domain ) {
    return domain switch {
      null => null,
      Domain.UndeclaredConnectionsPolicy.Blocked => UndeclaredConnectionsPolicy.Blocked,
      Domain.UndeclaredConnectionsPolicy.Reachable => UndeclaredConnectionsPolicy.Reachable,
      _ => throw new ArgumentOutOfRangeException( nameof(domain), domain, null )
    };
  }

  private static Network Map( Domain.Network domain ) {
    return new Network {
      Subnets = domain.Subnets.Select( Map ).ToList(), Devices = domain.Devices.Select( Map ).ToList()
    };
  }

  private static Subnet Map( DeclaredSubnet domain ) {
    return new Subnet { Id = domain.Id, Address = domain.Address, Enabled = domain.Enabled };
  }

  private static Device Map( DeclaredDevice domain ) {
    var identity = domain.Addresses.Where( a => a.IsId != false ).ToList();
    var info = domain.Addresses.Where( a => a.IsId == false ).ToList();

    return new Device {
      Id = domain.Id,
      Addresses = MapAddresses( identity ),
      Info = info.Count == 0 ? null : MapAddresses( info ),
      State = Map( domain.State ),
      Enabled = domain.Enabled
    };
  }

  private static DeviceState? Map( DeclaredDeviceState? domain ) {
    return domain switch {
      null => null,
      DeclaredDeviceState.Up => DeviceState.Up,
      DeclaredDeviceState.Dynamic => DeviceState.Dynamic,
      DeclaredDeviceState.Down => DeviceState.Down,
      _ => throw new ArgumentOutOfRangeException( nameof(domain), domain, null )
    };
  }

  private static Addresses MapAddresses( List<IDeviceAddress> domain ) {
    return new Addresses {
      Ipv4 = domain.OfType<IpV4Address>().Cast<IpV4Address?>().FirstOrDefault()?.Value,
      Mac = domain.OfType<MacAddress>().Cast<MacAddress?>().FirstOrDefault()?.Value,
      Hostname = domain.OfType<HostnameAddress>().Cast<HostnameAddress?>().FirstOrDefault()?.Value
    };
  }
}