using Drift.Domain;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageStreamManager : IAsyncDisposable {
  public IMessageStream GetOrCreate( Uri peerAddress, AgentId id );

  public IMessageStream Create(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context
  );
}