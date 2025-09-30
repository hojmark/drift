using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning;

//TODO read from /proc/net/arp instead of spawning processes
[SupportedOSPlatform( "linux" )]
internal static class LinuxArpCache {
  private static readonly Lock CacheLock = new();
  private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds( 1 );
  private static Dictionary<IPAddress, string> _arpCache = new();
  private static DateTime _lastUpdated = DateTime.MinValue;

  public static Dictionary<IPAddress, string> GetCachedTable() {
    return GetTable( forceRefresh: false );
  }

  public static Dictionary<IPAddress, string> GetFreshTable() {
    return GetTable( forceRefresh: true );
  }

  private static Dictionary<IPAddress, string> GetTable( bool forceRefresh ) {
    lock ( CacheLock ) {
      var now = DateTime.UtcNow;

      if ( !forceRefresh && ( now - _lastUpdated ) < CacheTtl ) {
        return _arpCache;
      }

      _arpCache = ReadSystemArpCache();
      _lastUpdated = now;
      return _arpCache;
    }
  }

  private static Dictionary<IPAddress, string> ReadSystemArpCache() {
    var map = new Dictionary<IPAddress, string>();

    var startInfo = new ProcessStartInfo {
      FileName = "arp",
      Arguments = "-en",
      RedirectStandardOutput = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var proc = Process.Start( startInfo );
    if ( proc == null )
      throw new InvalidOperationException( "Failed to start 'arp' process." );

    while ( !proc.StandardOutput.EndOfStream ) {
      var line = proc.StandardOutput.ReadLine();
      //Console.WriteLine( line );
      if ( string.IsNullOrWhiteSpace( line ) ) continue;
      if ( line.StartsWith( "Address" ) ) continue; // skip header

      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );

      // Defensive: expects at least Address, HWtype, HWaddress
      if ( parts.Length >= 3 &&
           parts[0].Count( c => c == '.' ) == 3 && // Looks like an IP
           parts[2].Contains( ':' ) ) // Looks like a MAC
      {
        var ip = parts[0];
        var mac = parts[2].ToUpperInvariant();

        IPAddress.TryParse( ip, out var ipParsedResult );
        var macParsed = new MacAddress( mac );
        map[ipParsedResult] = mac;
      }
    }

    return map;
  }
}