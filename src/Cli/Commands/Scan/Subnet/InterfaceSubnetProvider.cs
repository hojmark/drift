using System.Net.NetworkInformation;
using System.Net.Sockets;
using Drift.Cli.Output.Abstractions;
using Drift.Domain;
using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Subnet;

public class InterfaceSubnetProvider( IOutputManager output ) : IInterfaceSubnetProvider {
  public static List<System.Net.NetworkInformation.NetworkInterface> GetInterfacesRaw() {
    return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().ToList();
  }

  public virtual List<INetworkInterface> GetInterfaces() {
    return GetInterfacesRaw().Select( Map ).ToList();
  }

  private static INetworkInterface Map( System.Net.NetworkInformation.NetworkInterface networkInterface ) {
    var unicastAddress = GetIpV4UnicastAddress( networkInterface );
    return new NetworkInterface {
      Description = networkInterface.Description,
      OperationalStatus = networkInterface.OperationalStatus,
      UnicastAddress = unicastAddress == null ? null : GetCidrBlock( unicastAddress )
    };
  }

  public List<CidrBlock> Get() {
    var interfaces = GetInterfaces();
    var interfaceDescriptions =
      string.Join( ", ",
        interfaces.Select( i =>
          $"[{i.Description}, {( IsUp( i ) ? "up" : "down" )}, {( ( i.UnicastAddress == null )
            ? "-"
            : i.UnicastAddress )}]"
        )
      );
    output.Normal.WriteLineVerbose( $"Found interfaces: {interfaceDescriptions}" );
    output.Log.LogDebug( "Found interfaces: {Interfaces}", interfaceDescriptions );

    var cidrs = interfaces
      .Where( IsUp )
      .Where( i => i.UnicastAddress != null )
      .Select( i => i.UnicastAddress!.Value )
      .Where( cidrBlock =>
        IpNetworkUtils.IsPrivateIpV4( cidrBlock.NetworkAddress ) ) //TODO log if non-private networks were filtered
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

  private static UnicastIPAddressInformation?
    GetIpV4UnicastAddress( System.Net.NetworkInformation.NetworkInterface i ) {
    return i.GetIPProperties()
      .UnicastAddresses
      .SingleOrDefault( a => a.Address.AddressFamily == AddressFamily.InterNetwork );
  }

  private static bool IsUp( INetworkInterface i ) {
    return i.OperationalStatus == OperationalStatus.Up;
  }
}