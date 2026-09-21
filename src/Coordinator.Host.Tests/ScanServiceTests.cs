using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Coordinator.Services.Scans;
using Drift.Coordinator.Services.Spec;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;
using Drift.TestUtilities.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Coordinator.Host.Tests;

internal sealed class ScanServiceTests {
  [Test]
  public void Start_ReturnsQueuedScan() {
    var services = new ServiceCollection();
    services.AddScoped<IScanOrchestrator, TestScanOrchestrator>();
    services.AddScoped<IInterfaceSubnetProvider, EmptyInterfaceSubnetProvider>();
    using var provider = services.BuildServiceProvider();
    var service = new ScanService(
      provider.GetRequiredService<IServiceScopeFactory>(),
      new InMemoryAgentDirectory( Array.Empty<EnrolledAgent>() ),
      new CoordinatorSpec( new Inventory { Network = new() } ),
      NullLogger.Instance
    );

    var response = service.Start( new StartScanCommand() );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( response.ScanId, Is.Not.EqualTo( Guid.Empty ) );
      Assert.That( response.Status, Is.EqualTo( ScanStatus.Queued ) );
      Assert.That(
        service.Get( response.ScanId ).Status,
        Is.EqualTo( ScanStatus.Queued ).Or.EqualTo( ScanStatus.Running ).Or.EqualTo( ScanStatus.Completed )
      );
    }
  }

  [Test]
  public async Task Watch_ReturnsInitialAndCompletedEvents() {
    var services = new ServiceCollection();
    services.AddScoped<IScanOrchestrator, TestScanOrchestrator>();
    services.AddScoped<IInterfaceSubnetProvider, EmptyInterfaceSubnetProvider>();
    using var provider = services.BuildServiceProvider();
    var service = new ScanService(
      provider.GetRequiredService<IServiceScopeFactory>(),
      new InMemoryAgentDirectory( Array.Empty<EnrolledAgent>() ),
      new CoordinatorSpec( new Inventory { Network = new() } ),
      NullLogger.Instance
    );

    var response = service.Start( new StartScanCommand() );
    var events = new List<ScanEventData>();
    await foreach ( var scanEvent in service.Watch( response.ScanId, CancellationToken.None ) ) {
      events.Add( scanEvent );
    }

    Assert.That( events, Is.Not.Empty );
    Assert.That( events.Select( scanEvent => scanEvent.EventId ), Is.Ordered );
    Assert.That( events[^1].Status, Is.EqualTo( ScanStatus.Completed ) );
  }

  [Test]
  public async Task Watch_DeliversUpdatesPublishedAfterInitialSnapshotInOrder() {
    var orchestrator = new ControlledScanOrchestrator();
    var services = new ServiceCollection();
    services.AddScoped<IScanOrchestrator>( _ => orchestrator );
    services.AddScoped<IInterfaceSubnetProvider, EmptyInterfaceSubnetProvider>();
    using var provider = services.BuildServiceProvider();
    var service = new ScanService(
      provider.GetRequiredService<IServiceScopeFactory>(),
      new InMemoryAgentDirectory( Array.Empty<EnrolledAgent>() ),
      new CoordinatorSpec( new Inventory { Network = new() } ),
      NullLogger.Instance
    );

    var response = service.Start( new StartScanCommand { Cidrs = [new CidrBlock( "192.168.1.0/24" )] } );
    await orchestrator.Started.Task.WaitAsync( TimeSpan.FromSeconds( 1 ) );

    await using var events = service.Watch( response.ScanId, CancellationToken.None ).GetAsyncEnumerator();
    Assert.That( await events.MoveNextAsync(), Is.True );
    var initial = events.Current;

    orchestrator.PublishProgress();
    orchestrator.Complete();

    var remaining = new List<ScanEventData>();
    while ( await events.MoveNextAsync() ) {
      remaining.Add( events.Current );
    }

    var allEvents = new[] { initial }.Concat( remaining ).ToArray();
    Assert.That( allEvents.Select( scanEvent => scanEvent.EventId ), Is.EqualTo( new long[] { 1, 2, 3 } ) );
    Assert.That( allEvents.Select( scanEvent => scanEvent.Status ), Is.EqualTo(
      new[] { ScanStatus.Running, ScanStatus.Running, ScanStatus.Completed } ) );
  }

  [Test]
  public async Task Start_WarnsWhenDeclaredSubnetIsNotReachable() {
    var services = new ServiceCollection();
    services.AddScoped<IScanOrchestrator, TestScanOrchestrator>();
    services.AddScoped<IInterfaceSubnetProvider, EmptyInterfaceSubnetProvider>();
    using var provider = services.BuildServiceProvider();
    var logger = new StringLogger();
    var service = new ScanService(
      provider.GetRequiredService<IServiceScopeFactory>(),
      new InMemoryAgentDirectory( Array.Empty<EnrolledAgent>() ),
      new CoordinatorSpec(
        new Inventory {
          Network = new Network {
            Subnets = [new DeclaredSubnet { Address = "192.168.10.0/24" }]
          }
        }
      ),
      logger
    );

    var response = service.Start( new StartScanCommand() );
    for ( var attempt = 0; attempt < 100 && service.Get( response.ScanId ).Status is not ScanStatus.Completed; attempt++ ) {
      await Task.Delay( 10 );
    }

    Assert.That( logger.ToString(), Does.Contain( "declared subnet 192.168.10.0/24 is not reachable" ) );
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

  private sealed class ControlledScanOrchestrator : IScanOrchestrator {
    public TaskCompletionSource Started { get; } = new( TaskCreationOptions.RunContinuationsAsynchronously );
    private readonly TaskCompletionSource _completion = new( TaskCreationOptions.RunContinuationsAsynchronously );
    public event EventHandler<NetworkScanResult>? ResultUpdated;

    public async Task<NetworkScanResult> ScanAsync(
      NetworkScanOptions options,
      Microsoft.Extensions.Logging.ILogger logger,
      CancellationToken cancellationToken ) {
      Started.SetResult();
      await _completion.Task.WaitAsync( cancellationToken );
      return new NetworkScanResult {
        Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
        Progress = Percentage.Hundred,
        Status = ScanResultStatus.Success
      };
    }

    public void PublishProgress() {
      ResultUpdated?.Invoke( this, new NetworkScanResult {
        Metadata = new Metadata { StartedAt = DateTime.UtcNow },
        Progress = new Percentage( 50 ),
        Status = ScanResultStatus.InProgress
      } );
    }

    public void Complete() => _completion.SetResult();
  }

  private sealed class EmptyInterfaceSubnetProvider : IInterfaceSubnetProvider {
    public List<INetworkInterface> GetInterfaces() => [];

    public Task<List<ResolvedSubnet>> GetAsync() => Task.FromResult<List<ResolvedSubnet>>( [] );
  }
}
