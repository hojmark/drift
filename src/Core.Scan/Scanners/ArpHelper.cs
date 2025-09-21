using System.Diagnostics;

namespace Drift.Core.Scan.Scanners;

//TODO read from /proc/net/arp instead
internal sealed class ArpHelper {
  public static Dictionary<string, string> GetSystemCachedIpToMacMap() {
    var map = new Dictionary<string, string>();

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
        map[ip] = mac;
      }
    }

    return map;
  }
}