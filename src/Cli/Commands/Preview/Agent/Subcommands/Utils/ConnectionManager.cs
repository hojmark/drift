using System.Collections.Concurrent;
using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

public class ConnectionManager(
  ILogger logger,
  //Inventory inventory,
  PeerMessageHandlerDispatcher dispatcher,
  AgentPeers agentPeers
) : BackgroundService {
  // Tracks active connections
  private readonly ConcurrentDictionary<string, AgentConnection> _activeConnections = new();
  private string? SelfAddress = null;

  protected override async Task ExecuteAsync( CancellationToken cancellationToken ) {
    logger.LogInformation( "Starting outgoing connection handler" );
    await Task.Delay( 100, cancellationToken ); // Initial delay

    while ( !cancellationToken.IsCancellationRequested ) {
      /*foreach ( var agent in inventory.Agents ) {
        var address = agent.Address;

        // Skip if already connected
        if ( _activeConnections.ContainsKey( address ) || SelfAddress == address )
          continue;

        try {
          await TryConnectToAgent( address, cancellationToken );
        }
        catch ( Exception ex ) {
          logger.LogError( ex, "Failed to connect or communicate with {Address}", address );
        }
      }*/

      await Task.Delay( TimeSpan.FromSeconds( 5 ), cancellationToken );
    }
  }

  private async Task TryConnectToAgent( string address, CancellationToken cancellationToken ) {
    logger.LogInformation( "Attempting connection to {Address}", address );

    var (success, isSelf) = await PingAgent( address, cancellationToken );

    if ( !success ) {
      logger.LogWarning( "Ping to {Address} failed", address );
      return;
    }

    if ( isSelf == true ) {
      logger.LogDebug( "Ignoring self at {Address}", address );
      SelfAddress = address;
      return;
    }

    var channel = GrpcChannel.ForAddress( address );
    var client = new Drift.Networking.Grpc.Generated.PeerService.PeerServiceClient( channel );

    var call = client.PeerStream( cancellationToken: cancellationToken );

    logger.LogInformation( "Connected to peer at {Address}", address );

    var connection = new AgentConnection( channel, call );

    // Add to active connections
    if ( !_activeConnections.TryAdd( address, connection ) ) {
      // Unexpected: could not add — close connection
      logger.LogWarning( "Connection already existed for {Address}", address );
      await connection.DisposeAsync();
      return;
    }

    // Handle stream in background and monitor for termination
    _ = Task.Run( async () => {
      try {
        await PeerStreamSession.Run( call.ResponseStream, call.RequestStream, dispatcher, cancellationToken );
      }
      catch ( Exception ex ) {
        logger.LogWarning( ex, "Connection to {Address} lost", address );
      }
      finally {
        // Remove on disconnect
        if ( _activeConnections.TryRemove( address, out var closedConn ) ) {
          await closedConn.DisposeAsync();
          logger.LogInformation( "Disconnected from {Address}", address );
        }
      }
    }, cancellationToken );
  }

  private async Task<(bool Success, bool? IsSelf)> PingAgent( string address, CancellationToken cancellationToken ) {
    var channel = GrpcChannel.ForAddress( address );
    var client = new Drift.Networking.Grpc.Generated.PeerService.PeerServiceClient( channel );

    try {
      var response = await client.PingAsync( new Empty(), cancellationToken: cancellationToken );
      var isSelf = response.PeerId == AgentPeers.Self.Value.ToString();
      return ( true, isSelf );
    }
    catch ( RpcException ex ) {
      logger.LogTrace( ex, "Ping failed for {Address}", address );
      return ( false, null );
    }
  }

  // Helper class to manage connection resources
  private sealed class AgentConnection : IAsyncDisposable {
    private readonly GrpcChannel _channel;
    private readonly AsyncDuplexStreamingCall<PeerMessage, PeerMessage> _call;

    public AgentConnection( GrpcChannel channel, AsyncDuplexStreamingCall<PeerMessage, PeerMessage> call ) {
      _channel = channel;
      _call = call;
    }

    public async ValueTask DisposeAsync() {
      try {
        await _call.RequestStream.CompleteAsync();
      }
      catch {
        /* ignore */
      }

      try {
        await _call.ResponseStream.MoveNext( CancellationToken.None );
      }
      catch {
        /* ignore */
      }

      try {
        await _channel.ShutdownAsync();
      }
      catch {
        /* ignore */
      }
    }
  }
}