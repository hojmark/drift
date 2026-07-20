using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

internal class LinuxArpTableProvider : ArpTableProviderBase {
  private const string ProcArpPath = "/proc/net/arp";

  [SupportedOSPlatform( "linux" )]
  protected override ArpTable ReadSystemArpCache() {
    if ( !File.Exists( ProcArpPath ) ) {
      throw new FileNotFoundException( $"Path not found: {ProcArpPath}" );
    }

    using var streamReader = new StreamReader( ProcArpPath );

    return ParseArpOutput( streamReader );
  }

  /// <summary>
  /// Parses the contents of <c>/proc/net/arp</c> into an <see cref="ArpTable"/>.
  /// </summary>
  /// <remarks>
  /// Format:
  /// <code>
  /// IP address       HW type     Flags       HW address            Mask     Device
  /// 192.168.1.1      0x1         0x2         00:11:22:33:44:55     *        eth0
  /// </code>
  /// </remarks>
  internal static ArpTable ParseArpOutput( TextReader reader ) {
    var map = new Dictionary<IPAddress, MacAddress>();

    while ( reader.ReadLine() is { } line ) {
      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

      if (
        parts[0].Count( c => c == '.' ) != 3 && // Dots in an IPv4 address
        parts[1].Count( c => c == ':' ) != 5 // Semicolons in a Linux-reported MAC. E.g., 00:11:22:33:44:55
      ) {
        Console.Error.WriteLine( $"Skipping invalid ARP entry: {line}" );
        continue;
      }

      var ip = IPAddress.Parse( parts[0] );
      var mac = new MacAddress( parts[3] );
      map[ip] = mac;
    }

    return new ArpTable( map );
  }
}