using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

internal class LinuxArpTableProvider : ArpTableProviderBase {
  // TODO read from /proc/net/arp instead of spawning processes
  [SupportedOSPlatform( "linux" )]
  protected override ArpTable ReadSystemArpCache() {
    var startInfo = new ProcessStartInfo {
      FileName = "arp",
      Arguments = "-en",
      RedirectStandardOutput = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var proc = Process.Start( startInfo );
    if ( proc == null ) {
      throw new InvalidOperationException( "Failed to start 'arp' process." );
    }

    return ParseArpOutput( proc.StandardOutput );
  }

  /// <summary>
  /// Parses the output of <c>arp -en</c> on Linux into an <see cref="ArpTable"/>.
  /// </summary>
  /// <remarks>
  /// Linux <c>arp -en</c> output format (MAC is the third column):
  /// <code>
  /// Address          HWtype  HWaddress           Flags Mask  Iface
  /// 192.168.1.1      ether   00:11:22:33:44:55   C           eth0
  /// </code>
  /// </remarks>
  internal static ArpTable ParseArpOutput( TextReader reader ) {
    var map = new Dictionary<IPAddress, MacAddress>();

    string? line;
    while ( ( line = reader.ReadLine() ) != null ) {
      if ( string.IsNullOrWhiteSpace( line ) ) {
        continue;
      }

      if ( line.StartsWith( "Address" ) ) {
        continue; // skip header
      }

      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

      // Expects at least: Address, HWtype, HWaddress
      if ( parts.Length >= 3 &&
           parts[0].Count( c => c == '.' ) == 3 && // looks like an IPv4 address
           parts[2].Contains( ':' ) // Linux MACs use colons: 00:11:22:33:44:55
         ) {
        var ipParsed = IPAddress.Parse( parts[0] );
        map[ipParsed] = new MacAddress( parts[2] );
      }
    }

    return new ArpTable( map );
  }
}