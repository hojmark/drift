using Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Response;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Commands.Agent.Subcommands.Start.Peers.Messages.Subnets.Request;

internal class SubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  IPeerMessageEnvelopeConverter envelopeConverter,
  ILogger logger
) : IPeerMessageHandler {
  public string MessageType => "subnetsrequest";

  public async Task HandleAsync(
    PeerMessage message,
    IPeerStream peerStream,
    CancellationToken cancellationToken = default
  ) {
    logger.LogInformation( "Handling subnet request" );

    var subnets = await interfaceSubnetProvider.GetAsync();
    var response = new SubnetsResponse { Subnets = subnets };
    var envelope = envelopeConverter.ToEnvelope( response );
    envelope.ReplyTo = message.CorrelationId;

    logger.LogInformation( "Sending subnets: {Subnets}", string.Join( ", ", subnets ) );

    await peerStream.SendAsync( envelope );
  }
}