using Drift.Domain.Progress;

namespace Drift.Domain.Scan;

//TODO belongs to domain?
public interface INetworkScanner {
  public Task<ScanResult> ScanAsync(
    List<CidrBlock> cidrs,
    Action<ProgressReport>? onProgress = null,
    CancellationToken cancellationToken = default,
    int maxPingsPerSecond = 50
  );

  event EventHandler<ScanResult>? ResultUpdated;
}

public interface IScanService {
  Task<ScanResult> ScanAsync(
    ScanRequest request,
    CancellationToken cancellationToken = default
  );

  event EventHandler<ScanResult>? ResultUpdated;
  //event EventHandler<ScanLogEventArgs>? MessageLogged;
}

public class ScanRequest {
  public List<CidrBlock> Cidrs {
    get;
    init;
  } = new();

  public int MaxPingsPerSecond {
    get;
    init;
  } = 50;
}

public abstract class ScanResultEventArgs : EventArgs {
  public ScanResult IntermediateResult {
    get;
    init;
  }
}

public abstract class ScanLogEventArgs : EventArgs {
  public string Message {
    get;
    init;
  }

  //public LogLevel Level { get; init; }
}