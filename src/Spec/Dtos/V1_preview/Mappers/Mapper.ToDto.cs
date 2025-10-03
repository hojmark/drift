namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static partial class Mapper {
  internal const string VersionConstant = "v1-preview";

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