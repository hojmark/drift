using Drift.Domain;
using Drift.Domain.Progress;
using Drift.Domain.Scan;

namespace Drift.Cli.Tests.Utils;

public class PredefinedResultNetworkScanner( ScanResult scanResult ) : IScanService {
  public Task<ScanResult> ScanAsync( ScanRequest request, CancellationToken cancellationToken = default ) {
    return Task.FromResult( scanResult );
  }

  public event EventHandler<ScanResult>? ResultUpdated;
}