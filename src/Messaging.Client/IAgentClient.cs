using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Client;

public interface IAgentClient {
  Task<TResponse> RequestAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    // TODO why not always use a timeout?
    TimeSpan? timeout = null,
    // TODO eliminate default CancellationToken
    CancellationToken cancellationToken = default
  ) where TResponse : IResponse where TRequest : IRequest<TResponse>;

  Task<TResponse> RequestStreamingAsync<TRequest, TProgress, TResponse>(
    Domain.Agent agent,
    TRequest message,
    Action<TProgress> onProgress,
    // TODO why not always use a timeout?
    TimeSpan? timeout = null,
    // TODO eliminate default CancellationToken
    CancellationToken cancellationToken = default
  ) where TRequest : IStreamingRequest<TProgress, TResponse>
    where TProgress : IResponse
    where TResponse : IResponse;
}