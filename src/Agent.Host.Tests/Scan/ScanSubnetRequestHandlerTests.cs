using Drift.Agent.Host.Scan;
using Drift.Agent.Host.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Messaging.Protocol.Agent.Scan;
using Drift.Scanning.Scanners.Factories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Agent.Host.Tests.Scan;

internal sealed class ScanSubnetRequestHandlerTests {
  [Test]
  public async Task SendsProgressAndCompletion() {
    var cidr = new CidrBlock( "192.168.1.0/24" );
    var scanner = new StubSubnetScanner();
    var scannerFactory = new StubSubnetScannerFactory( scanner );
    var responder = new RecordingStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse>();

    await new ScanSubnetRequestHandler( scannerFactory, NullLogger.Instance ).HandleAsync(
      new ScanSubnetRequest { Cidr = cidr },
      responder,
      CancellationToken.None
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( responder.Progress.Single().ProgressPercentage, Is.EqualTo( 50 ) );
      Assert.That( responder.Progress.Single().DevicesFound, Is.Zero );
      Assert.That( responder.Responses.Single().Result.Status, Is.EqualTo( ScanResultStatus.Success ) );
      Assert.That( scanner.ResultUpdatedSubscriberCount, Is.Zero );
    }
  }

  [Test]
  public async Task PropagatesCancellationAndUnsubscribes() {
    var cidr = new CidrBlock( "192.168.1.0/24" );
    var scanner = new StubSubnetScanner { WaitForCompletion = true };
    var scannerFactory = new StubSubnetScannerFactory( scanner );
    var responder = new RecordingStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse>();
    using var cancellation = new CancellationTokenSource();

    var task = new ScanSubnetRequestHandler( scannerFactory, NullLogger.Instance ).HandleAsync(
      new ScanSubnetRequest { Cidr = cidr },
      responder,
      cancellation.Token
    );

    await scanner.ScanStarted;
    await cancellation.CancelAsync();
    await task;

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( scanner.ReceivedCancellationToken.IsCancellationRequested, Is.True );
      Assert.That( scanner.ResultUpdatedSubscriberCount, Is.Zero );
    }
  }

  private sealed class StubSubnetScannerFactory( ISubnetScanner scanner ) : ISubnetScannerFactory {
    public ISubnetScanner Get( CidrBlock cidr ) => scanner;
  }

  private sealed class StubSubnetScanner : ISubnetScanner {
    private readonly TaskCompletionSource _scanStarted = new(
      TaskCreationOptions.RunContinuationsAsynchronously
    );

    private EventHandler<SubnetScanResult>? _resultUpdated;

    public bool WaitForCompletion {
      get;
      init;
    }

    public Task ScanStarted => _scanStarted.Task;

    public CancellationToken ReceivedCancellationToken {
      get;
      private set;
    }

    public int ResultUpdatedSubscriberCount {
      get;
      private set;
    }

    public event EventHandler<SubnetScanResult>? ResultUpdated {
      add {
        _resultUpdated += value;
        ResultUpdatedSubscriberCount++;
      }
      remove {
        _resultUpdated -= value;
        ResultUpdatedSubscriberCount--;
      }
    }

    public async Task<SubnetScanResult> ScanAsync(
      SubnetScanOptions options,
      Microsoft.Extensions.Logging.ILogger logger,
      CancellationToken cancellationToken = default
    ) {
      ReceivedCancellationToken = cancellationToken;
      _scanStarted.SetResult();
      _resultUpdated?.Invoke( this, CreateResult( options.Cidr, 50, ScanResultStatus.InProgress ) );

      if ( WaitForCompletion ) {
        try {
          await Task.Delay( Timeout.InfiniteTimeSpan, cancellationToken );
        }
        catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
          return CreateResult( options.Cidr, 0, ScanResultStatus.Canceled );
        }
      }

      return CreateResult( options.Cidr, 100, ScanResultStatus.Success );
    }

    private static SubnetScanResult CreateResult( CidrBlock cidr, byte progress, ScanResultStatus status ) => new() {
      CidrBlock = cidr,
      Metadata = new Metadata { StartedAt = DateTime.UtcNow },
      Progress = new Percentage( progress ),
      Status = status
    };
  }
}