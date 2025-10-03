using System.Net.NetworkInformation;
using System.Net.Sockets;
using Drift.Common.Network;
using Drift.Domain;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Subnets.Interface;

public sealed class PhysicalInterfaceSubnetProvider( ILogger logger ) : InterfaceSubnetProviderBase( logger ) {
  public override List<INetworkInterface> GetInterfaces() {
    return GetPhysicalInterfaces().Select( Map ).ToList();
  }

  private static List<System.Net.NetworkInformation.NetworkInterface> GetPhysicalInterfaces() {
    return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().ToList();
  }

  private static INetworkInterface Map( System.Net.NetworkInformation.NetworkInterface networkInterface ) {
    var unicastAddress = GetIpV4UnicastAddress( networkInterface );

    return new NetworkInterface {
      Description = networkInterface.Description,
      OperationalStatus = networkInterface.OperationalStatus,
      UnicastAddress = unicastAddress == null ? null : GetCidrBlock( unicastAddress )
    };
  }

  private static CidrBlock GetCidrBlock( UnicastIPAddressInformation address ) {
    return new CidrBlock( IpNetworkUtils.GetNetworkAddress( address.Address, address.IPv4Mask ) + "/" +
                          IpNetworkUtils.GetCidrPrefixLength( address.IPv4Mask ) );
  }

  private static UnicastIPAddressInformation? GetIpV4UnicastAddress(
    System.Net.NetworkInformation.NetworkInterface networkInterface
  ) {
    return networkInterface.GetIPProperties()
      .UnicastAddresses
      .SingleOrDefault( a => a.Address.AddressFamily == AddressFamily.InterNetwork );
  }
}