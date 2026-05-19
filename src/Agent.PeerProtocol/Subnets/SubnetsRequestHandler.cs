using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.PeerProtocol.Subnets;

internal sealed class SubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ILogger logger
) : IPeerMessageHandler {
  public string MessageType => SubnetsRequest.MessageType;

  public async Task HandleAsync(
    PeerMessage envelope,
    IPeerMessageEnvelopeConverter converter,
    IPeerStream stream,
    CancellationToken cancellationToken
  ) {
    logger.LogInformation( "Handling subnet request" );

    var subnets = ( await interfaceSubnetProvider.GetAsync() ).Select( s => s.Cidr ).ToList();

    logger.LogInformation( "Sending subnets: {Subnets}", string.Join( ", ", subnets ) );

    var response = new SubnetsResponse { Subnets = subnets };
    await stream.SendResponseAsync( converter, response, envelope.CorrelationId );
  }
}