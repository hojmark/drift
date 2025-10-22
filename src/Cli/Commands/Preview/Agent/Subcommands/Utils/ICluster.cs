using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;

public interface ICluster {
  Task SendAsync( Domain.Agent agent, IPeerMessage message, CancellationToken cancellationToken = default );

  /*Task BroadcastAsync( PeerMessage message, CancellationToken cancellationToken = default );
  Task<List<CidrBlock>> RequestSubnetsAsync( string peerAddress, CancellationToken cancellationToken = default );
  Task EnsureConnectedAsync( string peerAddress, CancellationToken cancellationToken = default );
  IReadOnlyCollection<string> GetConnectedPeers();*/
}