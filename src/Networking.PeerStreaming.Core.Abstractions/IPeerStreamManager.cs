using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerStreamManager : IAsyncDisposable {
  public IPeerStream GetOrCreate( Uri peerAddress, AgentId id );

  public IPeerStream Create(
    IAsyncStreamReader<PeerMessage> requestStream,
    IAsyncStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  );
}