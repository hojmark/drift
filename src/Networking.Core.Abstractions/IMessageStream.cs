using Drift.Domain;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageStream : IAsyncDisposable {
  public int InstanceNo {
    get;
  }

  public AgentId RemoteId {
    get;
  }

  public Task ReadTask {
    get;
  }

  public Task SendAsync( Message message );
}