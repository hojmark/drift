using Drift.Networking.Grpc.Generated;
using Grpc.Core;

namespace Drift.Networking.Tests.Helpers;

internal static class TestServerCallContextExtensions {
  public static (
    DuplexStreamEndpoint<IClientStreamWriter<Message>, IAsyncStreamReader<Message>> Client,
    DuplexStreamEndpoint<IAsyncStreamReader<Message>, IServerStreamWriter<Message>> Server
    )
    CreateDuplexStreams( this TestServerCallContext serverContext ) {
    return InMemoryDuplexStreamPair.Create<Message, Message>( serverContext );
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