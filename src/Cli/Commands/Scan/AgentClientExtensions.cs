using Drift.Domain;
using Drift.Messaging.Client;
using Drift.Messaging.Protocol.Agent.Scan;
using Drift.Messaging.Protocol.Agent.Subnets;
using Drift.Networking.Core.Abstractions;

namespace Drift.Cli.Commands.Scan;

internal static class AgentClientExtensions {
  extension( IAgentClient agentClient ) {
    internal Task<SubnetsResponse> GetSubnetsAsync(
      Domain.Agent agent,
      CancellationToken cancellationToken
    ) {
      return agentClient.RequestAsync<SubnetsRequest, SubnetsResponse>(
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
      Action<ScanSubnetProgress> onProgress,
      CancellationToken cancellationToken
    ) {
      var request = new ScanSubnetRequest { Cidr = cidr, PingsPerSecond = pingsPerSecond };

      return agentClient.RequestStreamingAsync<
        ScanSubnetRequest,
        ScanSubnetProgress,
        ScanSubnetCompleteResponse
      >(
        agent,
        request,
        onProgress,
        timeout: TimeSpan.FromMinutes( 10 ),
        cancellationToken
      );
    }
  }
}