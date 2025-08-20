namespace Drift.Core.Abstractions;

public interface IScanService {
  public Task<ScanResponse> ExecuteScanAsync(
    ScanRequest request,
    CancellationToken cancellationToken = default
  );
}

public class ScanRequest {
}

public class ScanResponse {
}