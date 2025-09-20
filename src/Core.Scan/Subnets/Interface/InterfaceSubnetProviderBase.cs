using System.Net.NetworkInformation;
using Drift.Domain;
using Drift.Utils;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Subnets.Interface;

public abstract class InterfaceSubnetProviderBase( ILogger? logger ) : IInterfaceSubnetProvider {
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

    logger?.LogDebug( "Found interfaces: {Interfaces}", interfaceDescriptions );

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

    logger?.LogDebug( "Discovered subnet(s): {DiscoveredSubnets} (RFC1918 addresses only)",
      string.Join( ", ", cidrs ) );

    return cidrs;
  }

  private static bool IsUp( INetworkInterface i ) {
    return i.OperationalStatus == OperationalStatus.Up;
  }
}