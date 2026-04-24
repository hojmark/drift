using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;

namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static partial class Mapper {
  public static Inventory ToDomain( DriftSpec dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Address );
    var spec = new Inventory { Network = Map( dto.Network ) };

    if ( dto.Agents != null ) {
      spec.Agents = Map( dto.Agents );
    }

    return spec;
  }

  private static List<Domain.Agent> Map( List<Agent> dto ) {
    return dto.Select( Map ).ToList();
  }

  private static Domain.Agent Map( Agent dto ) {
    var agent = new Domain.Agent();

    agent.Id = dto.Id;
    agent.Address = dto.Address;

    return agent;
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

    var declaredDevice = new DeclaredDevice { Addresses = dto.Addresses.Select( Map ).ToList() };

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

  private static IDeviceAddress Map( DeviceAddress dto ) {
    return dto.Type switch {
      "ip-v4" => new Domain.Device.Addresses.IpV4Address( dto.Value, dto.IsId ?? true ),
      "mac" => new Domain.Device.Addresses.MacAddress( dto.Value, dto.IsId ?? true ),
      "hostname" => new Domain.Device.Addresses.HostnameAddress( dto.Value, dto.IsId ?? true ),
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto.Type, "Unknown address type" )
    };
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
}