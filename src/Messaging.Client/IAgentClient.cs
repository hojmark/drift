using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;

namespace Drift.Messaging.Client;

public interface IAgentClient {
  // Task SendAsync( Domain.Agent agent, IMessage message, CancellationToken cancellationToken = default );

  Task<TResponse> SendAndWaitAsync<TRequest, TResponse>(
    Domain.Agent agent,
    TRequest message,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TResponse : IResponse where TRequest : IRequest<TResponse>;

  Task<TFinalResponse> SendAndWaitStreamingAsync<TRequest, TFinalResponse>(
    Domain.Agent agent,
    TRequest message,
    string finalMessageType,
    Action<Message> onProgressUpdate,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default
  ) where TFinalResponse : IResponse where TRequest : IMessage;

  /*Task BroadcastAsync( Message message, CancellationToken cancellationToken = default );
  Task<List<CidrBlock>> RequestSubnetsAsync( string peerAddress, CancellationToken cancellationToken = default );
  Task EnsureConnectedAsync( string peerAddress, CancellationToken cancellationToken = default );
  IReadOnlyCollection<string> GetConnectedPeers();*/
}