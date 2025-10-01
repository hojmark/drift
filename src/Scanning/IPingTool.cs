using System.Net;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning;

public record PingResult( bool Success, int? RoundTripTimeMs = null );

public interface IPingTool {
  Task<PingResult> PingAsync( IPAddress ip, ILogger logger, CancellationToken cancellationToken = default );
}