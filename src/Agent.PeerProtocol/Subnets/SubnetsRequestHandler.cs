using System.Collections;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.PeerProtocol.Subnets;

internal sealed class SubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ILogger logger
) : IPeerMessageHandler<SubnetsRequest> {
  public string MessageType => "subnetsrequest";

  public async Task<IPeerMessage?> HandleAsync(
    SubnetsRequest message,
    CancellationToken cancellationToken = default
  ) {
    logger.LogInformation( "Handling subnet request" );

    var subnets = await interfaceSubnetProvider.GetAsync();
    var response = new SubnetsResponse { Subnets = subnets };

    logger.LogInformation( "Sending subnets: {Subnets}", string.Join( ", ", subnets ) );

    return response;
  }
}