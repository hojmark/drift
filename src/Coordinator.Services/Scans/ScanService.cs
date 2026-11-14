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
using RpcException = Grpc.Core.RpcException;

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
  private int _scanCounter;

  public bool Exists( Guid id ) => _scans.ContainsKey( id );

  public ScanStartResult Start( StartScanCommand command ) {
    var id = Guid.NewGuid();
    var scan = new ScanState( id, Interlocked.Increment( ref _scanCounter ) );
    _scans[id] = scan;
    logger.LogInformation(
      "Scan #{ScanNo} ({ScanId}) queued with {PingsPerSecond} pings per second",
      scan.No,
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
        "Scan #{ScanNo} started with {SubnetCount} requested subnets and {AgentCount} enrolled agents",
        scan.No,
        requestedCidrs.Count,
        enrolledAgents.Count
      );
      List<(EnrolledAgent Agent, CidrBlock Cidr)> remoteAssignments = [];
      if ( enrolledAgents.Count > 0 ) {
        remoteAssignments = await GetRemoteAssignmentsAsync(
          scope.ServiceProvider.GetRequiredService<AgentGateway>(),
          scan.No,
          enrolledAgents,
          requestedCidrs,
          cancellationToken
        );
      }

      var remoteCidrs = remoteAssignments.Select( assignment => assignment.Cidr ).ToHashSet();
      var localSubnets = await scope.ServiceProvider.GetRequiredService<IInterfaceSubnetProvider>().GetAsync();
      var localInterfaceCidrs = localSubnets.Select( subnet => subnet.Cidr ).ToHashSet();
      var reachableCidrs = localInterfaceCidrs.Concat( remoteCidrs ).ToHashSet();
      var unreachableCidrs = requestedCidrs.Except( reachableCidrs ).ToList();
      foreach ( var unreachableCidr in unreachableCidrs ) {
        logger.LogWarning(
          "Scan #{ScanNo}: declared subnet {Cidr} is not reachable locally or through an online agent; it will not be scanned",
          scan.No,
          unreachableCidr
        );
      }

      // Only scan subnets the coordinator is directly attached to. Subnets that belong to an offline agent must not be
      // scanned from here, as that would report the coordinator's view of the subnet as if it were the agent's.
      var localCidrs = requestedCidrs
        .Where( cidr => !remoteCidrs.Contains( cidr ) && localInterfaceCidrs.Contains( cidr ) )
        .ToList();
      var results = new List<SubnetScanResult>();
      results.AddRange( unreachableCidrs.Select( cidr => new SubnetScanResult {
        CidrBlock = cidr,
        Metadata = new Metadata { StartedAt = DateTime.UtcNow, EndedAt = DateTime.UtcNow },
        Status = ScanResultStatus.Error,
        Progress = Percentage.Zero
      } ) );
      logger.LogInformation(
        "Scan #{ScanNo} assigned {RemoteSubnetCount} subnets to agents and will scan {LocalSubnetCount} locally",
        scan.No,
        remoteAssignments.Count,
        localCidrs.Count
      );

      if ( localCidrs.Count > 0 ) {
        logger.LogInformation(
          "Scan #{ScanNo} starting local scan of {SubnetCount} subnets",
          scan.No,
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
            "Scan #{ScanNo} starting remote scan of {Cidr} on agent {AgentId}",
            scan.No,
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
        "Scan #{ScanNo} completed with {SubnetCount} subnet results and status {Status}",
        scan.No,
        results.Count,
        status
      );
    }
    catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
      scan.Update( ScanStatus.Cancelled, 0 );
      scan.Events.Writer.TryComplete();
    }
    catch ( Exception exception ) {
      logger.LogError( exception, "Scan #{ScanNo} failed", scan.No );
      scan.Update( ScanStatus.Failed, 0 );
      scan.Events.Writer.TryComplete();
    }
  }

  private async Task<List<(EnrolledAgent Agent, CidrBlock Cidr)>> GetRemoteAssignmentsAsync(
    AgentGateway agentGateway,
    int scanNo,
    IReadOnlyCollection<EnrolledAgent> enrolledAgents,
    IReadOnlyCollection<CidrBlock> requestedCidrs,
    CancellationToken cancellationToken
  ) {
    var assignments = new List<(EnrolledAgent Agent, CidrBlock Cidr)>();

    foreach ( var enrolledAgent in enrolledAgents ) {
      try {
        logger.LogDebug(
          "Scan #{ScanNo} querying agent {AgentId} for available subnets",
          scanNo,
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
          "Scan #{ScanNo}: agent {AgentId} reported {ReportedSubnetCount} subnets; assigned {AssignedSubnetCount}",
          scanNo,
          enrolledAgent.Id,
          response.Subnets.Count,
          assignedCidrs.Length
        );
      }
      catch ( OperationCanceledException ) when ( cancellationToken.IsCancellationRequested ) {
        throw;
      }
      catch ( Exception exception ) {
        logger.LogError(
          exception,
          "Scan #{ScanNo}: unable to query subnets from agent {AgentId} at {Address}; skipping agent. Its subnets will not be scanned unless the coordinator is directly attached to them. Reason: {Reason}",
          scanNo,
          enrolledAgent.Id,
          enrolledAgent.Address,
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

  internal static string GetRootCauseMessage( Exception exception ) {
    var rootCause = exception.GetBaseException();
    return rootCause switch {
      // gRPC keeps the underlying transport error in Status.DebugException rather than InnerException, and its Message
      // embeds the full status, e.g. Status(StatusCode="Unavailable", Detail="...", DebugException="...").
      RpcException { Status.DebugException: { } debugException } => debugException.GetBaseException().Message,
      RpcException rpcException => rpcException.Status.Detail,
      _ => rootCause.Message
    };
  }

  private sealed class ScanState( Guid id, int no ) {
    private readonly Lock _lock = new();
    private ScanStatus _status = ScanStatus.Queued;
    private byte _progress;
    private long _eventId;
    private NetworkScanResult? _result;

    public int No => no;
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