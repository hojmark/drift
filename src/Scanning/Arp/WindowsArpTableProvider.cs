using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

[SupportedOSPlatform( "windows" )]
internal class WindowsArpTableProvider : ArpTableProviderBase {
  protected override ArpTable ReadSystemArpCache() {
    var startInfo = new ProcessStartInfo {
      FileName = "arp",
      Arguments = "-a",
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
  /// Parses the output of <c>arp -a</c> on Windows into an <see cref="ArpTable"/>.
  /// </summary>
  /// <remarks>
  /// Windows <c>arp -a</c> separates MAC octets with hyphens, e.g.:
  /// <code>
  ///   192.168.1.1           00-11-22-33-44-55     dynamic
  /// </code>
  /// </remarks>
  internal static ArpTable ParseArpOutput( TextReader reader ) {
    var map = new Dictionary<IPAddress, MacAddress>();

    string? line;
    while ( ( line = reader.ReadLine() ) != null ) {
      if ( string.IsNullOrWhiteSpace( line ) ) {
        continue;
      }

      if ( line.StartsWith( "Interface" ) ) {
        continue; // skip section headers
      }

      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

      // Expects at least: Internet Address, Physical Address, Type
      if ( parts.Length >= 3 &&
           parts[0].Count( c => c == '.' ) == 3 && // looks like an IPv4 address
           parts[1].Contains( '-' ) // Windows MACs use hyphens: 00-11-22-33-44-55
         ) {
        var ipParsed = IPAddress.Parse( parts[0] );
        map[ipParsed] = new MacAddress( parts[1] );
      }
    }

    return new ArpTable( map );
  }
}