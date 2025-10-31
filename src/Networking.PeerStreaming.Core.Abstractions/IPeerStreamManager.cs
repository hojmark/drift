using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerStreamManager {
  public IPeerStream GetOrCreate( Uri peerAddress, string id );

  public IPeerStream Create(
    IAsyncStreamReader<PeerMessage> requestStream,
    IAsyncStreamWriter<PeerMessage> responseStream,
    ServerCallContext context
  );
}