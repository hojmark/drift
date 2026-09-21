using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;

namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static partial class Mapper {
  public static Inventory ToDomain( DriftSpec dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Address );
    var spec = new Inventory { Network = Map( dto.Network ) };

    if ( dto.Server != null ) {
      spec.Server = Map( dto.Server );
    }

    if ( dto.Settings != null ) {
      spec.Settings = Map( dto.Settings );
    }

    if ( dto.Agents != null ) {
      spec.Agents = Map( dto.Agents );
    }

    return spec;
  }

  private static Domain.Server Map( Server dto ) {
    return new Domain.Server { Address = dto.Address };
  }

  private static Domain.Settings Map( Settings dto ) {
    return new Domain.Settings {
      UnknownDevices = Map( dto.UnknownDevices ),
      UndeclaredConnections = Map( dto.UndeclaredConnections ),
      PingThrottling = dto.PingThrottling,
      ScanOnlyDeclaredSubnets = dto.ScanOnlyDeclaredSubnets
    };
  }

  private static Domain.UnknownDevicePolicy? Map( UnknownDevicePolicy? dto ) {
    return dto switch {
      null => null,
      UnknownDevicePolicy.Disallowed => Domain.UnknownDevicePolicy.Disallowed,
      UnknownDevicePolicy.Allowed => Domain.UnknownDevicePolicy.Allowed,
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto, null )
    };
  }

  private static Domain.UndeclaredConnectionsPolicy? Map( UndeclaredConnectionsPolicy? dto ) {
    return dto switch {
      null => null,
      UndeclaredConnectionsPolicy.Blocked => Domain.UndeclaredConnectionsPolicy.Blocked,
      UndeclaredConnectionsPolicy.Reachable => Domain.UndeclaredConnectionsPolicy.Reachable,
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto, null )
    };
  }

  private static List<Domain.Agent> Map( List<Agent> dto ) {
    return dto.Select( Map ).ToList();
  }

  private static Domain.Agent Map( Agent dto ) {
    var agent = new Domain.Agent();

    agent.Id = dto.Id;
    agent.Address = dto.Address;

    if ( dto.Policy != null ) {
      agent.Policy = dto.Policy.Select( Map ).ToList();
    }

    return agent;
  }

  private static Domain.Policy Map( Policy dto ) {
    return new Domain.Policy {
      To = dto.To,
      Expect = dto.Expect,
      Port = dto.Port,
      Protocol = dto.Protocol,
      Probe = dto.Probe,
      Fallback = dto.Fallback
    };
  }

  private static Domain.Network Map( Network dto ) {
    var network = new Domain.Network();

    if ( dto.Subnets != null ) {
      network.Subnets = Map( dto.Subnets );
    }

    if ( dto.Devices != null ) {
      network.Devices = Map( dto.Devices );
    }

    return network;
  }

  private static List<DeclaredSubnet> Map( List<Subnet> dto ) {
    return dto.Select( Map ).ToList();
  }

  private static DeclaredSubnet Map( Subnet dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Address );

    var subnet = new DeclaredSubnet { Address = dto.Address };

    if ( dto.Id != null ) {
      subnet.Id = dto.Id;
    }

    if ( dto.Enabled != null ) {
      subnet.Enabled = dto.Enabled;
    }

    return subnet;
  }

  private static List<DeclaredDevice> Map( List<Device> dto ) {
    return dto.Select( Map ).ToList();
  }

  private static DeclaredDevice Map( Device dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Addresses );

    var addresses = MapIdentityAddresses( dto.Addresses );

    if ( dto.Info != null ) {
      addresses.AddRange( MapInfoAddresses( dto.Info ) );
    }

    var declaredDevice = new DeclaredDevice { Addresses = addresses };

    if ( dto.Id != null ) {
      declaredDevice.Id = dto.Id;
    }

    if ( dto.State != null ) {
      declaredDevice.State = Map( dto.State );
    }

    if ( dto.Enabled != null ) {
      declaredDevice.Enabled = dto.Enabled;
    }

    return declaredDevice;
  }

  private static DeclaredDeviceState? Map( DeviceState? dto ) {
    return dto switch {
      null => null,
      DeviceState.Up => DeclaredDeviceState.Up,
      DeviceState.Dynamic => DeclaredDeviceState.Dynamic,
      DeviceState.Down => DeclaredDeviceState.Down,
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto, null )
    };
  }

  private static List<IDeviceAddress> MapIdentityAddresses( Addresses dto ) {
    var list = new List<IDeviceAddress>();

    if ( dto.Ipv4 != null ) {
      list.Add( new IpV4Address( dto.Ipv4, true ) );
    }

    if ( dto.Mac != null ) {
      list.Add( new MacAddress( dto.Mac, true ) );
    }

    if ( dto.Hostname != null ) {
      list.Add( new HostnameAddress( dto.Hostname, true ) );
    }

    return list;
  }

  private static List<IDeviceAddress> MapInfoAddresses( Addresses dto ) {
    var list = new List<IDeviceAddress>();

    if ( dto.Ipv4 != null ) {
      list.Add( new IpV4Address( dto.Ipv4, false ) );
    }

    if ( dto.Mac != null ) {
      list.Add( new MacAddress( dto.Mac, false ) );
    }

    if ( dto.Hostname != null ) {
      list.Add( new HostnameAddress( dto.Hostname, false ) );
    }

    return list;
  }
}