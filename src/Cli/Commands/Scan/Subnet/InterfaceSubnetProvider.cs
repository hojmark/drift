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

    output.Log.LogDebug( "Found interfaces (total): {Ifs}",
      string.Join( ", ", interfaces.Select( i => i.Description ) ) );

    var cidrs = interfaces
      .Where( IsUp )
      .Where( i => GetIpV4UnicastAddress( i ).Any() )
      .SelectMany( GetIpV4UnicastAddress )
      .Select( a =>
        new CidrBlock( IpNetworkUtils.GetNetworkAddress( a.Address, a.IPv4Mask ) + "/" +
                       IpNetworkUtils.GetCidrPrefixLength( a.IPv4Mask ) ) )
      .Where( a => IpNetworkUtils.IsPrivateIpV4( a.NetworkAddress ) ) //TODO log if non-private networks were filtered
      .ToList();

    //Console.WriteLine($"Interface: {ni.Name}");
    //Console.WriteLine($"Host address: {ipAddress}");
    //Console.WriteLine($"Network address: {networkAddress}/{prefixLength}");

    output.Normal.WriteLineVerbose( "Discovered subnets via interface: " + string.Join( ", ", cidrs ) );
    output.Log.LogDebug( "Discovered subnets via interface: {DiscoveredSubnets}", string.Join( ", ", cidrs ) );

    return cidrs;
  }

  private static IEnumerable<UnicastIPAddressInformation> GetIpV4UnicastAddress( NetworkInterface i ) {
    return i.GetIPProperties()
      .UnicastAddresses
      .Where( a => a.Address.AddressFamily == AddressFamily.InterNetwork );
  }

  private static bool IsUp( NetworkInterface i ) {
    return i.OperationalStatus == OperationalStatus.Up;
  }
}