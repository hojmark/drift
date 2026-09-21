using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageStreamManager : IAsyncDisposable {
  public IMessageStreamConnection GetOrCreate( Uri peerAddress, AgentId id );

  public IMessageStreamConnection Create(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context
  );
}