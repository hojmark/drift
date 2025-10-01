using System.Net;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Tests.Utils;

internal sealed class PredefinedPingTool( List<IPAddress> successful ) : IPingTool {
  public Task<PingResult> PingAsync(
    IPAddress ip,
    ILogger logger,
    CancellationToken cancellationToken = default
  ) {
    return Task.FromResult( new PingResult( successful.Contains( ip ) ) );
  }
}