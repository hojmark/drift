using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;

namespace Drift.Spec.Dtos.V1_preview.Mappers;

public static partial class Mapper {
  internal const string VersionConstant = "v1-preview";

  public static DriftSpec ToDto( Inventory domain ) {
    return new DriftSpec { Version = VersionConstant, Network = Map( domain.Network ) };
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
    return new Device {
      Id = domain.Id,
      Addresses = domain.Addresses.Select( Map ).ToList(),
      State = Map( domain.State ),
      Enabled = domain.Enabled
    };
  }

  private static DeviceAddress Map( IDeviceAddress domain ) {
    return new DeviceAddress { Value = domain.Value, Type = Map( domain.Type ), IsId = domain.IsId };
  }

  private static string Map( AddressType addressType ) {
    return addressType switch {
      AddressType.IpV4 => "ip-v4",
      AddressType.Mac => "mac",
      AddressType.Hostname => "hostname",
      _ => throw new ArgumentOutOfRangeException( nameof(addressType), addressType, null )
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
}