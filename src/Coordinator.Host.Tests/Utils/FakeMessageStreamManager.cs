using Drift.Domain;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Coordinator.Host.Tests.Utils;

internal sealed class FakeMessageStreamManager : IMessageStreamManager, IDisposable {
  public event Action<AgentId, ConnectionCloseOrigin>? ConnectionClosed;

  public IMessageStreamConnection GetOrCreate( Uri peerAddress, AgentId id ) {
    throw new NotSupportedException();
  }

  public IMessageStreamConnection Create(
    IAsyncStreamReader<Message> requestStream,
    IAsyncStreamWriter<Message> responseStream,
    ServerCallContext context
  ) {
    throw new NotSupportedException();
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  public void Dispose() {
  }

  public void Close( AgentId agentId, ConnectionCloseOrigin origin ) => ConnectionClosed?.Invoke( agentId, origin );
}