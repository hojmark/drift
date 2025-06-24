namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static class Mapper {
  internal const string VersionConstant = "v1-preview";

  public static Domain.Inventory ToDomain( DriftSpec dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Address );
    return new Domain.Inventory { Network = Map( dto.Network ) };
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

  private static List<Domain.DeclaredSubnet> Map( List<Subnet> dto ) {
    return dto.Select( Map ).ToList();
  }

  private static Domain.DeclaredSubnet Map( Subnet dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Address );

    var subnet = new Domain.DeclaredSubnet { Address = dto.Address };

    if ( dto.Id != null ) {
      subnet.Id = dto.Id;
    }

    if ( dto.Enabled != null ) {
      subnet.Enabled = dto.Enabled;
    }

    return subnet;
  }

  private static List<Domain.Device.Declared.DeclaredDevice> Map( List<Device> dto ) {
    return dto.Select( Map ).ToList();
  }

  private static Domain.Device.Declared.DeclaredDevice Map( Device dto ) {
    // ArgumentNullException.ThrowIfNull( dto.Addresses );

    var declaredDevice = new Domain.Device.Declared.DeclaredDevice { Addresses = dto.Addresses.Select( Map ).ToList() };

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

  private static Domain.Device.Addresses.IDeviceAddress Map( DeviceAddress dto ) {
    return dto.Type switch {
      "ip-v4" => new Domain.Device.Addresses.IpV4Address( dto.Value, dto.IsId ?? true ),
      "mac" => new Domain.Device.Addresses.MacAddress( dto.Value, dto.IsId ?? true ),
      "hostname" => new Domain.Device.Addresses.HostnameAddress( dto.Value, dto.IsId ?? true ),
      _ => throw new ArgumentOutOfRangeException( nameof(dto.Type), dto.Type, null )
    };
  }

  private static Domain.Device.Declared.DeclaredDeviceState? Map( DeviceState? dto ) {
    return dto switch {
      null => null,
      DeviceState.Up => Domain.Device.Declared.DeclaredDeviceState.Up,
      DeviceState.Dynamic => Drift.Domain.Device.Declared.DeclaredDeviceState.Dynamic,
      DeviceState.Down => Drift.Domain.Device.Declared.DeclaredDeviceState.Down,
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto, null )
    };
  }

  // ----------------------
  public static DriftSpec ToDto( Domain.Inventory domain ) {
    return new DriftSpec { Version = VersionConstant, Network = Map( domain.Network ) };
  }

  private static Network Map( Domain.Network domain ) {
    return new Network {
      Subnets = domain.Subnets.Select( Map ).ToList(), Devices = domain.Devices.Select( Map ).ToList()
    };
  }

  private static Subnet Map( Domain.DeclaredSubnet domain ) {
    return new Subnet { Id = domain.Id, Address = domain.Address, Enabled = domain.Enabled };
  }

  private static Device Map( Domain.Device.Declared.DeclaredDevice domain ) {
    return new Device {
      Id = domain.Id,
      Addresses = domain.Addresses.Select( Map ).ToList(),
      State = Map( domain.State ),
      Enabled = domain.Enabled
    };
  }

  private static DeviceAddress Map( Domain.Device.Addresses.IDeviceAddress domain ) {
    return new DeviceAddress { Value = domain.Value, Type = Map( domain.Type ), IsId = domain.IsId };
  }

  private static string Map( Domain.Device.Addresses.AddressType addressType ) {
    return addressType switch {
      Domain.Device.Addresses.AddressType.IpV4 => "ip-v4",
      Domain.Device.Addresses.AddressType.Mac => "mac",
      Domain.Device.Addresses.AddressType.Hostname => "hostname",
      _ => throw new ArgumentOutOfRangeException( nameof(addressType), addressType, null )
    };
  }

  private static DeviceState? Map( Domain.Device.Declared.DeclaredDeviceState? domain ) {
    return domain switch {
      null => null,
      Domain.Device.Declared.DeclaredDeviceState.Up => DeviceState.Up,
      Domain.Device.Declared.DeclaredDeviceState.Dynamic => DeviceState.Dynamic,
      Domain.Device.Declared.DeclaredDeviceState.Down => DeviceState.Down,
      _ => throw new ArgumentOutOfRangeException( nameof(domain), domain, null )
    };
  }
}