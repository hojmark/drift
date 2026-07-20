using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

internal class WindowsArpTableProvider : ArpTableProviderBase {
  [SupportedOSPlatform( "windows" )]
  protected override ArpTable ReadSystemArpCache() {
    var system32Path = Environment.GetFolderPath( Environment.SpecialFolder.System );
    var arpPath = Path.Combine( system32Path, "arp.exe" );
    var startInfo = new ProcessStartInfo {
      FileName = arpPath,
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
  /// Parses the output of <c>arp.exe -a</c> into an <see cref="ArpTable"/>.
  /// </summary>
  /// <remarks>
  /// Format:
  /// <code>
  /// [EMPTY LINE]
  /// Interface: 192.168.1.100 --- 0x7
  ///   Internet Address      Physical Address      Type
  ///   192.168.1.1           00-11-22-33-44-55     dynamic
  /// </code>
  /// </remarks>
  internal static ArpTable ParseArpOutput( TextReader reader ) {
    var map = new Dictionary<IPAddress, MacAddress>();

    reader.ReadLine(); // Skip empty line
    reader.ReadLine(); // Skip interface
    reader.ReadLine(); // Skip header

    while ( reader.ReadLine() is { } line ) {
      var parts = line.Split( (char[]?) null, StringSplitOptions.RemoveEmptyEntries );
      var ip = IPAddress.Parse( parts[0] );
      var mac = new MacAddress( parts[1] );
      map[ip] = mac;
    }

    return new ArpTable( map );
  }
}