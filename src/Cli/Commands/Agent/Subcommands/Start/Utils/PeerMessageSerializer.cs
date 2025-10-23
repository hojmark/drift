using System.Reflection;
using System.Text.Json;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.Grpc.Messages;

namespace Drift.Cli.Commands.Agent.Subcommands.Utils;

public class PeerMessageSerializer : IPeerMessageSerializer {
  private readonly Dictionary<string, Type> _typeMap = new();

  public PeerMessageSerializer( params Assembly[] assembliesToScan ) {
    var types = assembliesToScan
      .SelectMany( a => a.GetTypes() )
      .Where( t => typeof(IPeerMessage).IsAssignableFrom( t ) && !t.IsInterface && !t.IsAbstract );

    foreach ( var type in types ) {
      _typeMap[type.Name] = type;
    }
  }

  public PeerMessage ToEnvelope( IPeerMessage message, string? requestId = null ) {
    var json = JsonSerializer.Serialize( message, message.GetType() );
    return new PeerMessage { MessageType = message.GetType().Name, Message = json, };
  }

  public T FromEnvelope<T>( PeerMessage envelope ) where T : IPeerMessage {
    if ( !_typeMap.TryGetValue( envelope.MessageType, out var type ) ) {
      throw new InvalidOperationException( $"Unknown message type: {envelope.MessageType}" );
    }

    if ( type != typeof(T) ) {
      throw new InvalidOperationException(
        $"Message type mismatch: expected {typeof(T).Name}, got {envelope.MessageType}"
      );
    }

    return (T) JsonSerializer.Deserialize( envelope.Message, type )!;
  }
}