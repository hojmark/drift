using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

[SupportedOSPlatform( "windows" )]
internal class WindowsArpTableProvider : ArpTableProviderBase {
  protected override ArpTable ReadSystemArpCache() {
    var map = new Dictionary<IPAddress, MacAddress>();

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

    while ( !proc.StandardOutput.EndOfStream ) {
      var line = proc.StandardOutput.ReadLine();
      // Console.WriteLine( line );
      if ( string.IsNullOrWhiteSpace( line ) ) {
        continue;
      }

      if ( line.StartsWith( "Interface" ) ) {
        continue; // skip header
      }

      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

      // Defensive: expects at least Internet Address, Physical Address, Type
      if ( parts.Length >= 3 &&
           parts[0].Count( c => c == '.' ) == 3 && // Looks like an IP
           parts[1].Contains( ':' ) // Looks like a MAC
         ) {
        var ip = parts[0];
        var mac = parts[1].ToUpperInvariant();

        var ipParsed = IPAddress.Parse( ip );
        map[ipParsed] = new MacAddress( mac );
      }
    }

    return new ArpTable( map );
  }
}