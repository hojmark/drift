using Drift.Messaging.Protocol.Subnets;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Host.Subnets;

internal sealed class SubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ILogger logger
) : IMessageHandler {
  public string MessageType => SubnetsRequest.MessageType;

  public async Task HandleAsync(
    Message envelope,
    IMessageEnvelopeConverter converter,
    IMessageStream stream,
    CancellationToken cancellationToken
  ) {
    logger.LogInformation( "Handling subnet request" );

    var subnets = ( await interfaceSubnetProvider.GetAsync() ).Select( s => s.Cidr ).ToList();

    logger.LogInformation( "Sending subnets: {Subnets}", string.Join( ", ", subnets ) );

    var response = new SubnetsResponse { Subnets = subnets };
    await stream.SendResponseAsync( converter, response, envelope.CorrelationId );
  }
}