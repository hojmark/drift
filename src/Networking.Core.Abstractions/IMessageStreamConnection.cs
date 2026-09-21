using Drift.Domain;
using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Core.Abstractions;

/// <summary>
/// Represents the shared messaging state for one active peer connection.
/// </summary>
public interface IMessageStreamConnection : IAsyncDisposable {
  AgentId RemoteId {
    get;
  }

  Uri? RemoteAddress {
    get;
  }

  ConnectionSide Side {
    get;
  }

  IMessageStream Stream {
    get;
  }

  Task Completion {
    get;
  }

  Task<Message> WaitForResponseAsync( RequestId requestId, TimeSpan timeout, CancellationToken cancellationToken );

  Task<Message> WaitForStreamingResponseAsync(
    RequestId requestId,
    string finalMessageType,
    Action<Message> onProgressUpdate,
    TimeSpan timeout,
    CancellationToken cancellationToken
  );
}