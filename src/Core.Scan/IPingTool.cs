using System.Net;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan;

public record PingResult( bool Success, int? RoundTripTimeMs = null );

public interface IPingTool {
  /* public Task<(string StdOut, string ErrOut, int ExitCode, bool Cancelled)> RunAsync(
     string arguments,
     ILogger? logger = null
   );*/

  Task<PingResult> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken = default );
}