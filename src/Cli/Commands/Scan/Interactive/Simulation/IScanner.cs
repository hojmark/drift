namespace Drift.Cli.Commands.Scan.Interactive.Simulation;

public interface IScanner {
  /// Metadata for the scan: label, duration, subnets
  ScanSession Session {
    get;
  }

  /// Starts the scanning process (real or simulated)
  void Start();

  /// Returns the current state of the scan (can be partial)
  List<Subnet> GetCurrentSubnets();

  /// True if all data has been revealed / scan is done
  bool IsComplete {
    get;
  }

  uint Progress {
    get;
  }
}