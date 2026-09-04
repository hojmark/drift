using System.Collections.Concurrent;
using System.Threading.Channels;
using Drift.Domain;
using Drift.Domain.Scan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Coordinator.Host.Apis.Control;

// TODO prototype
internal sealed class ControlService {
  private readonly ConcurrentDictionary<string, AgentSummary> _agents = new();
  private readonly ConcurrentDictionary<string, ScanState> _scans = new();
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger _logger;

  public ControlService( IServiceScopeFactory scopeFactory, ILogger logger ) {
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  public IReadOnlyCollection<AgentSummary> GetAgents() => _agents.Values.ToArray();

  public bool Exists( string id ) => _scans.ContainsKey( id );

  public StartScanResponse StartScan( StartScanRequest request ) {
    var id = Guid.NewGuid().ToString( "N" );
    var scan = new ScanState( id );
    _scans[id] = scan;
    _ = RunScanAsync( scan, request, CancellationToken.None );
    return new StartScanResponse( id, "queued" );
  }

  public ScanStatusResponse GetScan( string id ) {
    return _scans.TryGetValue( id, out var scan )
      ? scan.ToResponse()
      : throw new KeyNotFoundException( $"Scan '{id}' was not found." );
  }

  public NetworkScanResult GetResult( string id ) {
    if ( !_scans.TryGetValue( id, out var scan ) || scan.Result == null ) {
      throw new KeyNotFoundException( $"Completed result for scan '{id}' was not found." );
    }

    return scan.Result;
  }

  public async IAsyncEnumerable<ScanEvent> WatchScan(
    string id,
    [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken ) {
    if ( !_scans.TryGetValue( id, out var scan ) ) {
      throw new KeyNotFoundException( $"Scan '{id}' was not found." );
    }

    yield return scan.ToEvent();
    await foreach ( var scanEvent in scan.Events.Reader.ReadAllAsync( cancellationToken ) ) {
      yield return scanEvent;
    }
  }

  private async Task RunScanAsync( ScanState scan, StartScanRequest request, CancellationToken cancellationToken ) {
    await using var scope = _scopeFactory.CreateAsyncScope();
    var scanOrchestrator = scope.ServiceProvider.GetRequiredService<IScanOrchestrator>();

    try {
      scan.Update( "running", 0 );
      var options = new NetworkScanOptions {
        Cidrs = request.Cidrs?.Select( cidr => new CidrBlock( cidr ) ).ToList() ?? [],
        PingsPerSecond = request.PingsPerSecond
      };
      EventHandler<NetworkScanResult> progress = ( _, result ) => scan.Update( "running", result.Progress.Value );
      scanOrchestrator.ResultUpdated += progress;
      try {
        var result = await scanOrchestrator.ScanAsync( options, _logger, cancellationToken );
        scan.Complete( result );
      }
      finally {
        scanOrchestrator.ResultUpdated -= progress;
      }
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      scan.Update( "cancelled", 0 );
      scan.Events.Writer.TryComplete();
    }
    catch ( Exception exception ) {
      _logger.LogError( exception, "Control scan {ScanId} failed", scan.Id );
      scan.Update( "failed", 0 );
      scan.Events.Writer.TryComplete();
    }
  }

  private sealed class ScanState( string id ) {
    private readonly object _gate = new();
    private string _status = "queued";
    private byte _progress;
    private NetworkScanResult? _result;

    public string Id => id;
    public NetworkScanResult? Result => _result;

    public Channel<ScanEvent> Events {
      get;
    } = Channel.CreateUnbounded<ScanEvent>();

    public ScanStatusResponse ToResponse() {
      lock ( _gate ) {
        return new(id, _status, _progress);
      }
    }

    public ScanEvent ToEvent() {
      lock ( _gate ) {
        return new(id, _status, _progress, _result);
      }
    }

    public void Update( string status, byte progress ) {
      lock ( _gate ) {
        _status = status;
        _progress = progress;
        Events.Writer.TryWrite( new ScanEvent( id, status, progress ) );
      }
    }

    public void Complete( NetworkScanResult result ) {
      lock ( _gate ) {
        _result = result;
        _status = "completed";
        _progress = result.Progress.Value;
        Events.Writer.TryWrite( new ScanEvent( id, _status, _progress, result ) );
        Events.Writer.TryComplete();
      }
    }
  }
}