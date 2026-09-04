using Drift.Coordinator.Host.Apis.Control;
using Drift.Domain;
using Drift.Domain.Scan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class ControlServiceTests {
  [Test]
  public void StartScan_ReturnsQueuedScan() {
    var services = new ServiceCollection();
    services.AddScoped<IScanOrchestrator, TestScanOrchestrator>();
    using var provider = services.BuildServiceProvider();
    var service = new ControlService( provider.GetRequiredService<IServiceScopeFactory>(), NullLogger.Instance );

    var response = service.StartScan( new StartScanRequest() );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( response.ScanId, Is.Not.Empty );
      Assert.That( response.Status, Is.EqualTo( "queued" ) );
      Assert.That(
        service.GetScan( response.ScanId ).Status,
        Is.EqualTo( "queued" ).Or.EqualTo( "running" ).Or.EqualTo( "completed" )
      );
    }
  }

  private sealed class TestScanOrchestrator : IScanOrchestrator {
    public event EventHandler<NetworkScanResult>? ResultUpdated;

    public Task<NetworkScanResult> ScanAsync(
      NetworkScanOptions options,
      Microsoft.Extensions.Logging.ILogger logger,
      CancellationToken cancellationToken
    ) {
      var result = new NetworkScanResult {
        Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
        Progress = Percentage.Hundred,
        Status = ScanResultStatus.Success
      };
      ResultUpdated?.Invoke( this, result );
      return Task.FromResult( result );
    }
  }
}