using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Core.Common;
using Drift.Networking.Core.Messages;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Core;

/// <summary>
/// Owns the state and lifetime of one active bidirectional peer connection.
/// </summary>
internal sealed class Connection : IMessageStreamConnection {
  private readonly MessageResponseCorrelator _correlator;
  private readonly AsyncServiceScope _scope;
  private readonly CancellationTokenSource? _connectionCancellation;
  private readonly ILogger _logger;
  private int _disposeStarted;
  private int _closedLocally;

  private Connection(
    IMessageStream stream,
    MessageResponseCorrelator correlator,
    AsyncServiceScope scope,
    CancellationTokenSource? connectionCancellation,
    ILogger logger
  ) {
    Stream = stream;
    _correlator = correlator;
    _scope = scope;
    _connectionCancellation = connectionCancellation;
    _logger = logger;
    LogCreated();
  }

  public IMessageStream Stream {
    get;
  }

  public Task Completion => Stream.ReadTask;

  private bool WasClosedLocally => Volatile.Read( ref _closedLocally ) != 0;

  internal void MarkClosedLocally() {
    Interlocked.Exchange( ref _closedLocally, 1 );
  }

  public static Connection CreateOutbound(
    Uri remoteAddress,
    AgentId remoteId,
    IMessagingClientFactory messageClientFactory,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    MessagingOptions options
  ) {
    var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource( options.StoppingToken );
    var (client, _) = messageClientFactory.Create( remoteAddress );
    var callOptions = new CallOptions( new Metadata { { GrpcMetadataExtensions.AgentIdKey, remoteId } } );
    var call = client.Connect( callOptions );
    var scope = scopeFactory.CreateAsyncScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<MessageDispatcher>();
    var correlator = scope.ServiceProvider.GetRequiredService<MessageResponseCorrelator>();
    var stream = new MessageStream(
      remoteAddress,
      call.ResponseStream,
      call.RequestStream,
      dispatcher,
      logger,
      options.StoppingToken
    ) { RemoteId = remoteId };

    return new Connection( stream, correlator, scope, connectionCancellation, logger );
  }

  public static Connection CreateInbound(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    MessagingOptions options
  ) {
    var remoteId = context.RequestHeaders.GetAgentId();
    var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(
      options.StoppingToken,
      context.CancellationToken
    );
    var scope = scopeFactory.CreateAsyncScope();
    var dispatcher = scope.ServiceProvider.GetRequiredService<MessageDispatcher>();
    var correlator = scope.ServiceProvider.GetRequiredService<MessageResponseCorrelator>();
    var stream = new MessageStream(
      requestStream,
      responseStream,
      dispatcher,
      logger,
      connectionCancellation.Token
    ) { RemoteId = remoteId };

    return new Connection( stream, correlator, scope, connectionCancellation, logger );
  }

  public Task<Message> WaitForResponseAsync(
    RequestId requestId,
    TimeSpan timeout,
    CancellationToken cancellationToken
  ) {
    return _correlator.WaitForResponseAsync( requestId, timeout, cancellationToken );
  }

  public Task<Message> WaitForStreamingResponseAsync(
    RequestId requestId,
    string finalMessageType,
    Action<Message> onProgressUpdate,
    TimeSpan timeout,
    CancellationToken cancellationToken
  ) {
    return _correlator.WaitForStreamingResponseAsync(
      requestId,
      finalMessageType,
      onProgressUpdate,
      timeout,
      cancellationToken
    );
  }

  public async ValueTask DisposeAsync() {
    MarkClosedLocally();
    await DisposeCoreAsync();
  }

  internal async ValueTask DisposeAfterCompletionAsync() {
    await DisposeCoreAsync();
  }

  private async ValueTask DisposeCoreAsync() {
    if ( Interlocked.Exchange( ref _disposeStarted, 1 ) != 0 ) {
      return;
    }

    _correlator.FailPendingRequests(
      new IOException( $"Messaging stream for agent '{Stream.RemoteId}' was closed." )
    );
    try {
      await Stream.DisposeAsync();
    }
    finally {
      LogDisposed();
      try {
        await _scope.DisposeAsync();
      }
      finally {
        _connectionCancellation?.Dispose();
      }
    }
  }

  private void LogCreated() {
    var direction = Stream.Side == ConnectionSide.Outbound ? "to" : "from";
    _logger.LogDebug(
      "{ConnectionSide} stream #{StreamNo} {Direction} agent {AgentId} created",
      Stream.Side,
      Stream.InstanceNo,
      direction,
      Stream.RemoteId
    );
  }

  private void LogDisposed() {
    var direction = Stream.Side == ConnectionSide.Outbound ? "to" : "from";
    var message = WasClosedLocally
      ? "{ConnectionSide} stream #{StreamNo} {Direction} agent {AgentId} disposed locally"
      : "{ConnectionSide} stream #{StreamNo} {Direction} agent {AgentId} disposed";
    _logger.LogDebug( message, Stream.Side, Stream.InstanceNo, direction, Stream.RemoteId );
  }
}