using System.Net.NetworkInformation;
using Drift.Cli.Output.Abstractions;
using Drift.Domain;
using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Subnet;

public abstract class InterfaceSubnetProviderBase( IOutputManager output ) : IInterfaceSubnetProvider {
  public abstract List<INetworkInterface> GetInterfaces();

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

  private static bool IsUp( INetworkInterface i ) {
    return i.OperationalStatus == OperationalStatus.Up;
  }
}