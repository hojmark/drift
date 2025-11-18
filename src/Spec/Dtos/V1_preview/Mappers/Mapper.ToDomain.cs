namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static partial class Mapper {
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
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto.Type, "Unknown address type" )
    };
  }

  private static Domain.Device.Declared.DeclaredDeviceState? Map( DeviceState? dto ) {
    return dto switch {
      null => null,
      DeviceState.Up => Domain.Device.Declared.DeclaredDeviceState.Up,
      DeviceState.Dynamic => Domain.Device.Declared.DeclaredDeviceState.Dynamic,
      DeviceState.Down => Domain.Device.Declared.DeclaredDeviceState.Down,
      _ => throw new ArgumentOutOfRangeException( nameof(dto), dto, null )
    };
  }
}