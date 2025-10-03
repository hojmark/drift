using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

// TODO read from /proc/net/arp instead of spawning processes
[SupportedOSPlatform( "linux" )]
internal class LinuxArpTableProvider : IArpTableProvider {
  private static readonly Lock CacheLock = new();
  private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds( 1 );
  private static ArpTable _cache = ArpTable.Empty;
  private static DateTime _lastUpdated = DateTime.MinValue;

  public ArpTable Cached => GetTable( forceRefresh: false );

  public ArpTable Fresh => GetTable( forceRefresh: true );

  private static ArpTable GetTable( bool forceRefresh ) {
    lock ( CacheLock ) {
      var now = DateTime.UtcNow;

      if ( !forceRefresh && ( now - _lastUpdated ) < CacheTtl ) {
        return _cache;
      }

      _cache = ReadSystemArpCache();
      _lastUpdated = now;
      return _cache;
    }
  }

  private static ArpTable ReadSystemArpCache() {
    var map = new Dictionary<IPAddress, MacAddress>();

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

    while ( !proc.StandardOutput.EndOfStream ) {
      var line = proc.StandardOutput.ReadLine();
      // Console.WriteLine( line );
      if ( string.IsNullOrWhiteSpace( line ) ) {
        continue;
      }

      if ( line.StartsWith( "Address" ) ) {
        continue; // skip header
      }

      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

      // Defensive: expects at least Address, HWtype, HWaddress
      if ( parts.Length >= 3 &&
           parts[0].Count( c => c == '.' ) == 3 && // Looks like an IP
           parts[2].Contains( ':' ) // Looks like a MAC
         ) {
        var ip = parts[0];
        var mac = parts[2].ToUpperInvariant();

        var ipParsed = IPAddress.Parse( ip );
        map[ipParsed] = new MacAddress( mac );
      }
    }

    return new ArpTable( map );
  }
}