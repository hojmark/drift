using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Scan;
using Drift.Messaging.Protocol.Subnets;
using Drift.Networking.Core.Abstractions;

namespace Drift.Cli.Commands.Scan;

internal static class AgentClientExtensions {
  extension( IAgentClient agentClient ) {
    internal Task<SubnetsResponse> GetSubnetsAsync(
      Domain.Agent agent,
      CancellationToken cancellationToken
    ) {
      return agentClient.SendAndWaitAsync<SubnetsRequest, SubnetsResponse>(
        agent,
        new SubnetsRequest(),
        timeout: TimeSpan.FromSeconds( 10 ),
        cancellationToken
      );
    }

    internal Task<ScanSubnetCompleteResponse> ScanSubnetAsync(
      Domain.Agent agent,
      CidrBlock cidr,
      uint pingsPerSecond,
      IMessageEnvelopeConverter converter,
      Action<ScanSubnetProgressUpdate> onProgressUpdate,
      CancellationToken cancellationToken
    ) {
      var request = new ScanSubnetRequest { Cidr = cidr, PingsPerSecond = pingsPerSecond };

      return agentClient.SendAndWaitStreamingAsync<ScanSubnetRequest, ScanSubnetCompleteResponse>(
        agent,
        request,
        finalMessageType: ScanSubnetCompleteResponse.MessageType,
        progressEnvelope => {
          // Deserialize progress update and call handler
          if ( progressEnvelope.MessageType == ScanSubnetProgressUpdate.MessageType ) {
            var progressUpdate = converter.FromEnvelope<ScanSubnetProgressUpdate>( progressEnvelope );
            onProgressUpdate( progressUpdate );
          }
        },
        timeout: TimeSpan.FromMinutes( 10 ),
        cancellationToken
      );
    }
  }
}