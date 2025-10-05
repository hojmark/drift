using System.Text.Json;
using Drift.EnvironmentConfig.Converters;
using Drift.Networking.Grpc.Generated;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Networking.PeerStreaming.Core.Messages;

internal sealed class PeerMessageEnvelopeConverter : IPeerMessageEnvelopeConverter {
  private readonly Dictionary<string, Type> _typeMap = new();
  private readonly JsonSerializerOptions _serializerOptions;

  public PeerMessageEnvelopeConverter( IPeerMessageTypesProvider provider ) : this( provider.Get() ) {
  }

  public PeerMessageEnvelopeConverter( params Type[] messageTypes ) : this( (IEnumerable<Type>) messageTypes ) {
  }

  private PeerMessageEnvelopeConverter( IEnumerable<Type> messageTypes ) {
    foreach ( var type in messageTypes ) {
      // TODO improve
      var instance = (IPeerMessage) Activator.CreateInstance( type )!;
      _typeMap[instance.MessageType] = type;
    }

    _serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    _serializerOptions.Converters.Add( new IpAddressConverter() );
    _serializerOptions.Converters.Add( new CidrBlockConverter() );
  }

  public PeerMessage ToEnvelope( IPeerMessage message, string? requestId = null ) {
    var json = JsonSerializer.Serialize( message, message.GetType(), _serializerOptions );
    return new PeerMessage { MessageType = message.MessageType, Message = json, };
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

    return (T) JsonSerializer.Deserialize( envelope.Message, type, _serializerOptions )!;
  }
}