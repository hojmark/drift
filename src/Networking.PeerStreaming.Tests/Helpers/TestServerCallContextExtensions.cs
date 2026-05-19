using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.PeerStreaming.Tests.Helpers;

internal static class TestServerCallContextExtensions {
  public static (
    DuplexStreamEndpoint<IClientStreamWriter<PeerMessage>, IAsyncStreamReader<PeerMessage>> Client,
    DuplexStreamEndpoint<IAsyncStreamReader<PeerMessage>, IServerStreamWriter<PeerMessage>> Server
    )
    CreateDuplexStreams( this TestServerCallContext serverContext ) {
    return InMemoryDuplexStreamPair.Create<PeerMessage, PeerMessage>( serverContext );
  }

  internal static (
    DuplexStreamEndpoint<IClientStreamWriter<TRequest>, IAsyncStreamReader<TResponse>> Client,
    DuplexStreamEndpoint<IAsyncStreamReader<TRequest>, IServerStreamWriter<TResponse>> Server
    )
    CreateDuplexStreams<TRequest, TResponse>( TestServerCallContext serverContext ) where TRequest : class
    where TResponse : class {
    return InMemoryDuplexStreamPair.Create<TRequest, TResponse>( serverContext );
  }
}