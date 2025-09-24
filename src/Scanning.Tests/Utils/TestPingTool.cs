using System.Net;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Tests.Utils;

internal sealed class TestPingTool( List<IPAddress> successful ) : IPingTool {
  /* public Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
     string arguments,
     bool? logCommand,
     ILogger? logger = null
   ) {
     // Find IPv4 address in arguments string
     var match = Regex.Match( arguments, @"\b(?:(?:25[0-5]|2[0-4][0-9]|1\d{2}|[1-9]?\d)(?:\.|$)){4}\b" );

     if ( match.Success ) {
       if ( IPAddress.TryParse( match.Value, out var ip ) ) {
         bool success = successful.Contains( ip );

         return Task.FromResult( (
           StdOut: success ? $"Ping to {ip} succeeded" : $"Ping to {ip} failed",
           ErrOut: "",
           ExitCode: success ? 0 : 1,
           Cancelled: false
         ) );
       }
     }

     // If no IPv4 address found, treat as failure
     return Task.FromResult( (
       StdOut: "",
       ErrOut: "No valid IPv4 address found in arguments",
       ExitCode: 1,
       Cancelled: false
     ) );
   }*/

  public Task<PingResult> PingAsync(
    IPAddress ip,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    return Task.FromResult( new PingResult( successful.Contains( ip ) ) );
  }
}