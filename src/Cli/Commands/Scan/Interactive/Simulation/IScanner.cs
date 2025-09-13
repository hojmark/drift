using Drift.Domain.Scan;

namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public interface IScanner {
  /// Metadata for the scan: label, duration, subnets
  ScanSession Session {
    get;
  }

  /// Starts the scanning process (real or simulated)
  void Start();
  

  uint Progress {
    get;
  }
  
  public event EventHandler<List<Subnet>>? SubnetsUpdated;
}

public interface IScanServiceTWO {
  Task<ScanResult> ScanAsync(
    ScanRequest request,
    CancellationToken cancellationToken = default
  );

  event EventHandler<ScanResult>? ResultUpdate;
  event EventHandler<ScanLogEventArgs>? MessageLogged;
}