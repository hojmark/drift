using Drift.Messaging.Protocol.Agent.Subnets;
using Drift.Networking.Core.Abstractions;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Host.Subnets;

internal sealed class InterfaceSubnetsRequestHandler(
  IInterfaceSubnetProvider interfaceSubnetProvider,
  ILogger logger
) : RequestHandler<SubnetsRequest, SubnetsResponse>( logger ) {
  public override async Task HandleAsync(
    SubnetsRequest request,
    IMessageResponder<SubnetsResponse> responder,
    CancellationToken cancellationToken
  ) {
    var subnets = ( await interfaceSubnetProvider.GetAsync() ).Select( s => s.Cidr ).ToList();

    Logger.LogInformation( "Sending subnets: {Subnets}", string.Join( ", ", subnets ) );

    var response = new SubnetsResponse { Subnets = subnets };
    await responder.SendAsync( response );
  }
}