using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Networking.Clustering;

public interface ICluster {
  Task SendAsync( Domain.Agent agent, IPeerMessage message, CancellationToken cancellationToken = default );

  Task<TResponse> SendAndWaitAsync<TResponse>(
    Domain.Agent agent,
    IPeerMessage message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IPeerMessage;

  /*Task BroadcastAsync( PeerMessage message, CancellationToken cancellationToken = default );
  Task<List<CidrBlock>> RequestSubnetsAsync( string peerAddress, CancellationToken cancellationToken = default );
  Task EnsureConnectedAsync( string peerAddress, CancellationToken cancellationToken = default );
  IReadOnlyCollection<string> GetConnectedPeers();*/
}