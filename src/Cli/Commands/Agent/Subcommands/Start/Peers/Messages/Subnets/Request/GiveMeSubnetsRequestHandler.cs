using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Response;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming;
using Drift.Networking.PeerStreaming.Messages;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;

internal class GiveMeSubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  IPeerMessageEnvelopeConverter envelopeConverter,
  ILogger logger
) : IPeerMessageHandler {
  public string? MessageType => "subnetrequest";

  public async Task HandleAsync( PeerMessage message, PeerStream peerStream,
    CancellationToken cancellationToken = default ) {
    logger.LogInformation( "Received subnet request from peer" );

    try {
      // Get subnets from local interfaces
      var subnets = await interfaceSubnetProvider.GetAsync();

      logger.LogInformation( "Found {Count} subnets to send", subnets.Count );

      // Create response message
      var response = new GiveMeSubnetsResponse { Subnets = subnets };

      // Convert to envelope and set reply correlation
      var envelope = envelopeConverter.ToEnvelope( response );
      envelope.ReplyTo = message.CorrelationId;

      // Send response back through the peer stream
      await peerStream.SendAsync( envelope );

      logger.LogInformation( "Sent {Count} subnets back to peer", subnets.Count );
    }
    catch ( Exception ex ) {
      logger.LogError( ex, "Failed to handle subnet request" );
      throw;
    }
  }
}