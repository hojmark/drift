using System.Net.NetworkInformation;
using System.Net.Sockets;
using Drift.Cli.Output.Abstractions;
using Drift.Domain;
using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Subnet;

internal class InterfaceSubnetProvider( IOutputManager output ) : ISubnetProvider {
  public List<CidrBlock> Get() {
    var interfaces = NetworkInterface.GetAllNetworkInterfaces();

    var interfaceDescriptions =
      string.Join( ", ",
        interfaces.Select( i =>
          $"[{i.Description}, {( IsUp( i ) ? "up" : "down" )}, {( ( GetIpV4UnicastAddress( i ) == null )
            ? "-"
            : GetCidrBlock( GetIpV4UnicastAddress( i )! ) )}]"
        )
      );
    output.Normal.WriteLineVerbose( $"Found interfaces: {interfaceDescriptions}" );
    output.Log.LogDebug( "Found interfaces: {Interfaces}", interfaceDescriptions );

    var cidrs = interfaces
      .Where( IsUp )
      .Where( i => GetIpV4UnicastAddress( i ) != null )
      .Select( GetIpV4UnicastAddress )
      .Select( GetCidrBlock! )
      .Where( a => IpNetworkUtils.IsPrivateIpV4( a.NetworkAddress ) ) //TODO log if non-private networks were filtered
      .Distinct() // Maybe return <interface, cidr> tuple?
      .ToList();

    //Console.WriteLine($"Interface: {ni.Name}");
    //Console.WriteLine($"Host address: {ipAddress}");
    //Console.WriteLine($"Network address: {networkAddress}/{prefixLength}");

    output.Normal.WriteLineVerbose( $"Discovered subnet(s): {string.Join( ", ", cidrs )} (RFC1918 addresses only)" );
    output.Log.LogDebug( "Discovered subnet(s): {DiscoveredSubnets} (RFC1918 addresses only)",
      string.Join( ", ", cidrs ) );

    return cidrs;
  }

  private static CidrBlock GetCidrBlock( UnicastIPAddressInformation a ) {
    return new CidrBlock( IpNetworkUtils.GetNetworkAddress( a.Address, a.IPv4Mask ) + "/" +
                          IpNetworkUtils.GetCidrPrefixLength( a.IPv4Mask ) );
  }

  private static UnicastIPAddressInformation? GetIpV4UnicastAddress( NetworkInterface i ) {
    return i.GetIPProperties()
      .UnicastAddresses
      .SingleOrDefault( a => a.Address.AddressFamily == AddressFamily.InterNetwork );
  }

  private static bool IsUp( NetworkInterface i ) {
    return i.OperationalStatus == OperationalStatus.Up;
  }
}