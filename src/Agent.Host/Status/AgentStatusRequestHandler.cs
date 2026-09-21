using Drift.Messaging.Protocol.Agent.Status;
using Drift.Networking.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Agent.Host.Status;

internal sealed class AgentStatusRequestHandler( ILogger logger )
  : RequestHandler<AgentStatusRequest, AgentStatusResponse>( logger ) {
  public override async Task HandleAsync(
    AgentStatusRequest request,
    IMessageResponder<AgentStatusResponse> responder,
    CancellationToken cancellationToken
  ) {
    await responder.SendAsync( new AgentStatusResponse { Status = AgentStatus.Ready } );
  }
}