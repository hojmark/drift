using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Drift.Coordinator.Services.Agents;
using Drift.Coordinator.Services.Models;
using Drift.Coordinator.Services.Spec;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Messaging.Protocol.Agent.Scan;
using Drift.Messaging.Protocol.Agent.Subnets;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Coordinator.Services.Scans;

/// <summary>
/// Coordinates scan execution and the temporary state exposed by the scan API.
/// </summary>
public sealed class ScanService(
  IServiceScopeFactory scopeFactory,
  IAgentDirectory agentDirectory,
  CoordinatorSpec coordinatorSpec,
  ILogger logger
) {
  private readonly ConcurrentDictionary<Guid, ScanState> _scans = new();

  public bool Exists( Guid id ) => _scans.ContainsKey( id );

  public ScanStartResult Start( StartScanCommand command ) {
    var id = Guid.NewGuid();
    var scan = new ScanState( id );
    _scans[id] = scan;
    logger.LogInformation(
      "Coordinator scan {ScanId} queued with {PingsPerSecond} pings per second",
      id,
      command.PingsPerSecond
    );
    _ = RunAsync( scan, command, CancellationToken.None );
    return new ScanStartResult( id, ScanStatus.Queued );
  }

  public ScanStatusResult Get( Guid id ) {
    return _scans.TryGetValue( id, out var scan )
      ? scan.ToResponse()
      : throw new KeyNotFoundException( $"Scan '{id}' was not found." );
  }

  public NetworkScanResult GetResult( Guid id ) {
    if ( !_scans.TryGetValue( id, out var scan ) || scan.Result == null ) {
      throw new KeyNotFoundException( $"Completed result for scan '{id}' was not found." );
    }

    return scan.Result;
  }

  public JsonElement GetResultJson( Guid id ) {
    return JsonSerializer.SerializeToElement( GetResult( id ), ScanJsonSerializerContext.Default.NetworkScanResult );
  }

  public async IAsyncEnumerable<ScanEventData> Watch(
    Guid id,
    [System.Runtime.CompilerServices.EnumeratorCancellation]
    CancellationToken cancellationToken ) {
    if ( !_scans.TryGetValue( id, out var scan ) ) {
      throw new KeyNotFoundException( $"Scan '{id}' was not found." );
    }

    var initialEvent = scan.ToEvent();
    yield return initialEvent;

    await foreach ( var scanEvent in scan.Events.Reader.ReadAllAsync( cancellationToken ) ) {
      if ( scanEvent.EventId <= initialEvent.EventId ) {
        continue;
      }

      yield return scanEvent;
    }
  }

  private async Task RunAsync( ScanState scan, StartScanCommand command, CancellationToken cancellationToken ) {
    await using var scope = scopeFactory.CreateAsyncScope();
    var scanOrchestrator = scope.ServiceProvider.GetRequiredService<IScanOrchestrator>();

    try {
      scan.Update( ScanStatus.Running, 0 );
      var requestedCidrs = command.Cidrs?.ToList() ?? coordinatorSpec.View.Network.Subnets
        .Where( subnet => subnet.Enabled != false )
        .Select( subnet => new CidrBlock( subnet.Address ) )
        .ToList();
      var enrolledAgents = agentDirectory.GetEnrolledAgents();
      logger.LogInformation(
        "Coordinator scan {ScanId} started with {SubnetCount} requested subnets and {AgentCount} enrolled agents",
        scan.Id,
        requestedCidrs.Count,
        enrolledAgents.Count
      );
      List<(EnrolledAgent Agent, CidrBlock Cidr)> remoteAssignments = [];
      if ( enrolledAgents.Count > 0 ) {
        remoteAssignments = await GetRemoteAssignmentsAsync(
          scope.ServiceProvider.GetRequiredService<AgentGateway>(),
          scan.Id,
          enrolledAgents,
          requestedCidrs,
          cancellationToken
        );
      }

      var remoteCidrs = remoteAssignments.Select( assignment => assignment.Cidr ).ToHashSet();
      var localSubnets = await scope.ServiceProvider.GetRequiredService<IInterfaceSubnetProvider>().GetAsync();
      var reachableCidrs = localSubnets.Select( subnet => subnet.Cidr ).Concat( remoteCidrs ).ToHashSet();
      var unreachableCidrs = requestedCidrs.Except( reachableCidrs ).ToList();
      foreach ( var unreachableCidr in unreachableCidrs ) {
        logger.LogWarning(
          "Coordinator scan {ScanId}: declared subnet {Cidr} is not reachable locally or through an enrolled agent",
          scan.Id,
          unreachableCidr
        );
      }

      var localCidrs = requestedCidrs.Where( cidr => !remoteCidrs.Contains( cidr ) ).ToList();
      var results = new List<SubnetScanResult>();
      results.AddRange( unreachableCidrs.Select( cidr => new SubnetScanResult {
        CidrBlock = cidr,
        Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
        Status = ScanResultStatus.Error,
        Progress = Percentage.Zero
      } ) );
      logger.LogInformation(
        "Coordinator scan {ScanId} assigned {RemoteSubnetCount} subnets to agents and will scan {LocalSubnetCount} locally",
        scan.Id,
        remoteAssignments.Count,
        localCidrs.Count
      );

      if ( localCidrs.Count > 0 ) {
        logger.LogInformation(
          "Coordinator scan {ScanId} starting local scan of {SubnetCount} subnets",
          scan.Id,
          localCidrs.Count
        );
        var options = new NetworkScanOptions { Cidrs = localCidrs, PingsPerSecond = command.PingsPerSecond };
        EventHandler<NetworkScanResult> progress = ( _, result ) =>
          scan.Update( ScanStatus.Running, result.Progress.Value );
        scanOrchestrator.ResultUpdated += progress;
        try {
          results.AddRange( ( await scanOrchestrator.ScanAsync( options, logger, cancellationToken ) ).Subnets );
        }
        finally {
          scanOrchestrator.ResultUpdated -= progress;
        }
      }

      if ( remoteAssignments.Count > 0 ) {
        var agentGateway = scope.ServiceProvider.GetRequiredService<AgentGateway>();
        foreach ( var assignment in remoteAssignments ) {
          logger.LogInformation(
            "Coordinator scan {ScanId} starting remote scan of {Cidr} on agent {AgentId}",
            scan.Id,
            assignment.Cidr,
            assignment.Agent.Id
          );
          var response =
            await agentGateway.RequestStreamingAsync<ScanSubnetRequest, ScanSubnetProgress, ScanSubnetResponse>(
              assignment.Agent.Id,
              new ScanSubnetRequest { Cidr = assignment.Cidr, PingsPerSecond = command.PingsPerSecond },
              progress => scan.Update( ScanStatus.Running, progress.ProgressPercentage ),
              TimeSpan.FromMinutes( 10 ),
              cancellationToken
            );
          results.Add( response.Result );
        }
      }

      var status = results.All( result => result.Status == ScanResultStatus.Success )
        ? ScanResultStatus.Success
        : ScanResultStatus.Error;
      scan.Complete( new NetworkScanResult {
        Subnets = results,
        Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
        Status = status,
        Progress = Percentage.Hundred
      } );
      logger.LogInformation(
        "Coordinator scan {ScanId} completed with {SubnetCount} subnet results and status {Status}",
        scan.Id,
        results.Count,
        status
      );
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      scan.Update( ScanStatus.Cancelled, 0 );
      scan.Events.Writer.TryComplete();
    }
    catch ( Exception exception ) {
      logger.LogError( exception, "Control scan {ScanId} failed", scan.Id );
      scan.Update( ScanStatus.Failed, 0 );
      scan.Events.Writer.TryComplete();
    }
  }

  private async Task<List<(EnrolledAgent Agent, CidrBlock Cidr)>> GetRemoteAssignmentsAsync(
    AgentGateway agentGateway,
    Guid scanId,
    IReadOnlyCollection<EnrolledAgent> enrolledAgents,
    IReadOnlyCollection<CidrBlock> requestedCidrs,
    CancellationToken cancellationToken
  ) {
    var assignments = new List<(EnrolledAgent Agent, CidrBlock Cidr)>();

    foreach ( var enrolledAgent in enrolledAgents ) {
      try {
        logger.LogDebug(
          "Coordinator scan {ScanId} querying agent {AgentId} for available subnets",
          scanId,
          enrolledAgent.Id
        );
        var response = await agentGateway.RequestAsync<SubnetsRequest, SubnetsResponse>(
          enrolledAgent.Id,
          new SubnetsRequest(),
          TimeSpan.FromSeconds( 10 ),
          cancellationToken
        );
        var assignedCidrs = response.Subnets.Where( requestedCidrs.Contains ).ToArray();
        assignments.AddRange( assignedCidrs.Select( cidr => ( Agent: enrolledAgent, Cidr: cidr ) ) );
        logger.LogInformation(
          "Coordinator scan {ScanId} agent {AgentId} reported {ReportedSubnetCount} subnets; assigned {AssignedSubnetCount}",
          scanId,
          enrolledAgent.Id,
          response.Subnets.Count,
          assignedCidrs.Length
        );
      }
      catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
        throw;
      }
      catch ( Exception exception ) {
        LogStaleAgent(
          enrolledAgent,
          GetRootCauseMessage( exception )
        );
      }
    }

    return assignments
      .GroupBy( assignment => assignment.Cidr )
      .Select( group => group.First() )
      .OrderBy( assignment => assignment.Cidr.ToString(), StringComparer.Ordinal )
      .ToList();
  }

  private void LogStaleAgent( EnrolledAgent agent, string reason ) {
    logger.LogWarning(
      "Unable to query subnets from agent {AgentId} at {Address}; scanning its subnets locally. Reason: {Reason}",
      agent.Id,
      agent.Address,
      reason
    );
  }

  private static string GetRootCauseMessage( Exception exception ) {
    return exception.GetBaseException().Message;
  }

  private sealed class ScanState( Guid id ) {
    private readonly object _lock = new();
    private ScanStatus _status = ScanStatus.Queued;
    private byte _progress;
    private long _eventId;
    private NetworkScanResult? _result;

    public Guid Id => id;
    public NetworkScanResult? Result => _result;
    public long EventId => Interlocked.Read( ref _eventId );

    public Channel<ScanEventData> Events {
      get;
    } = Channel.CreateUnbounded<ScanEventData>();

    public ScanStatusResult ToResponse() {
      lock ( _lock ) {
        return new(id, _status, _progress);
      }
    }

    public ScanEventData ToEvent() {
      lock ( _lock ) {
        JsonElement? result = _result is null
          ? null
          : JsonSerializer.SerializeToElement( _result, ScanJsonSerializerContext.Default.NetworkScanResult );
        return new(EventId, id, _status, _progress, result);
      }
    }

    public void Update( ScanStatus status, byte progress ) {
      lock ( _lock ) {
        _status = status;
        _progress = progress;
        var scanEvent = new ScanEventData( ++_eventId, id, status, progress );
        Events.Writer.TryWrite( scanEvent );
      }
    }

    public void Complete( NetworkScanResult result ) {
      lock ( _lock ) {
        _result = result;
        _status = ScanStatus.Completed;
        _progress = result.Progress.Value;
        var scanEvent = new ScanEventData(
          ++_eventId,
          id,
          _status,
          _progress,
          JsonSerializer.SerializeToElement( result, ScanJsonSerializerContext.Default.NetworkScanResult )
        );
        Events.Writer.TryWrite( scanEvent );
        Events.Writer.TryComplete();
      }
    }
  }
}