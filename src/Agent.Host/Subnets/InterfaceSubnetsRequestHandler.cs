using Drift.Domain;
using Drift.Messaging.Protocol.Agent.Subnets;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Host.Subnets;

internal sealed class InterfaceSubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ILogger logger
) : MessageHandler<SubnetsRequest, SubnetsResponse> {
  public override async Task HandleAsync(
    SubnetsRequest request,
    IMessageResponder<SubnetsResponse> responder,
    CancellationToken cancellationToken
  ) {
    logger.LogInformation( "Handling subnet request" );

    var subnets = ( await interfaceSubnetProvider.GetAsync() ).Select( s => s.Cidr ).ToList();

    logger.LogInformation( "Sending subnets: {Subnets}", string.Join( ", ", subnets ) );

    var response = new SubnetsResponse { Subnets = subnets };
    await responder.SendAsync( response );
  }
}