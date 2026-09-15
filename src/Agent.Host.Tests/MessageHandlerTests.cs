using System.Reflection;
using Drift.Agent.Host.Scan;
using Drift.Agent.Host.Subnets;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Messaging.Protocol.Agent.Scan;
using Drift.Messaging.Protocol.Agent.Subnets;
using Drift.Networking.Core.Abstractions;
using Drift.Scanning.Scanners.Factories;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Agent.Host.Tests;

internal sealed class MessageHandlerTests {
  private static readonly Assembly HandlersAssembly = typeof(AgentHost).Assembly;
  private static readonly IEnumerable<Type> HandlerTypes = GetAllConcreteHandlerTypes();

  [Test]
  public void FindMessagesAndHandlers() {
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( HandlerTypes.ToList(), Has.Count.GreaterThan( 1 ), "No handlers found via reflection" );
    }
  }

  [Test]
  public async Task SubnetsRequest_ReturnsInterfaceSubnets() {
    // Arrange
    var cidr = new CidrBlock( "192.168.1.0/24" );
    var interfaceProvider = new StubInterfaceSubnetProvider( [new ResolvedSubnet( cidr, SubnetSource.Local )] );
    var typedResponder = new RecordingMessageResponder<SubnetsResponse>();

    // Act
    await new InterfaceSubnetsRequestHandler( interfaceProvider, NullLogger.Instance ).HandleAsync(
      new SubnetsRequest(),
      typedResponder,
      CancellationToken.None
    );

    // Assert
    Assert.That( typedResponder.Responses.Single().Subnets, Is.EqualTo( [cidr] ) );
  }

  [Test]
  public async Task ScanSubnetRequest_SendsProgressAndCompletion() {
    // Arrange
    var cidr = new CidrBlock( "192.168.1.0/24" );
    var scanner = new StubSubnetScanner();
    var responder = new RecordingStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse>();

    // Act
    await new ScanSubnetRequestHandler( new StubSubnetScannerFactory( scanner ), NullLogger.Instance ).HandleAsync(
      new ScanSubnetRequest { Cidr = cidr },
      responder,
      CancellationToken.None
    );

    // Assert
    Assert.That( responder.Progress, Has.Count.EqualTo( 1 ) );
    Assert.That( responder.Completions, Has.Count.EqualTo( 1 ) );
    Assert.That( scanner.ResultUpdatedSubscriberCount, Is.EqualTo( 0 ) );
    Assert.That( responder.Completions.Single().Result.Status, Is.EqualTo( ScanResultStatus.Success ) );
  }

  [Test]
  public async Task ScanSubnetRequest_PropagatesCancellationAndUnsubscribes() {
    // Arrange
    var scanner = new StubSubnetScanner { WaitForCompletion = true };
    using var cancellation = new CancellationTokenSource();
    var cidr = new CidrBlock( "192.168.1.0/24" );
    var responder = new RecordingStreamingMessageResponder<ScanSubnetProgress, ScanSubnetResponse>();
    var task = new ScanSubnetRequestHandler( new StubSubnetScannerFactory( scanner ), NullLogger.Instance ).HandleAsync(
      new ScanSubnetRequest { Cidr = cidr },
      responder,
      cancellation.Token
    );

    // Act
    await WaitUntilAsync( () => scanner.ReceivedCancellationToken.CanBeCanceled );
    await cancellation.CancelAsync();
    await task;

    // Assert
    Assert.That( scanner.ReceivedCancellationToken.IsCancellationRequested, Is.True );
    Assert.That( scanner.ResultUpdatedSubscriberCount, Is.EqualTo( 0 ) );
  }

  private static List<Type> GetAllConcreteHandlerTypes() {
    return HandlersAssembly
      .GetTypes()
      .Where( t => t is { IsAbstract: false, IsInterface: false } )
      .Where( t => typeof(IMessageHandler).IsAssignableFrom( t ) )
      .ToList();
  }

  private static async Task WaitUntilAsync( Func<bool> condition ) {
    for ( var i = 0; i < 100 && !condition(); i++ ) {
      await Task.Delay( 10 );
    }

    Assert.That( condition(), Is.True );
  }

  private sealed class RecordingMessageResponder<TResponse> : IMessageResponder<TResponse>
    where TResponse : IResponse {
    public List<TResponse> Responses {
      get;
    } = [];

    public Task SendAsync( TResponse response ) {
      Responses.Add( response );
      return Task.CompletedTask;
    }
  }

  private sealed class RecordingStreamingMessageResponder<TProgress, TFinalResponse>
    : IStreamingMessageResponder<TProgress, TFinalResponse>
    where TProgress : IResponse
    where TFinalResponse : IResponse {
    public List<TProgress> Progress {
      get;
    } = [];

    public List<TFinalResponse> Completions {
      get;
    } = [];

    public Task SendAsync( TFinalResponse response ) {
      Completions.Add( response );
      return Task.CompletedTask;
    }

    public void SendProgress( TProgress progress ) {
      Progress.Add( progress );
    }
  }

  private sealed class StubInterfaceSubnetProvider( List<ResolvedSubnet> subnets ) : IInterfaceSubnetProvider {
    public Task<List<ResolvedSubnet>> GetAsync() => Task.FromResult( subnets );
    public List<INetworkInterface> GetInterfaces() => [];
  }

  private sealed class StubSubnetScannerFactory( ISubnetScanner scanner ) : ISubnetScannerFactory {
    public ISubnetScanner Get( CidrBlock cidr ) => scanner;
  }

  private sealed class StubSubnetScanner : ISubnetScanner {
    private EventHandler<SubnetScanResult>? _resultUpdated;

    public bool WaitForCompletion {
      get;
      init;
    }

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