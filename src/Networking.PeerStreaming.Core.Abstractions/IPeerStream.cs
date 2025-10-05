using Drift.Domain;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerStream : IAsyncDisposable {
  public int InstanceNo {
    get;
  }

  public AgentId AgentId {
    get;
  }

  public Task ReadTask {
    get;
  }

  public Task SendAsync( PeerMessage message );
}