using Drift.Networking.Grpc.Generated;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageEnvelopeConverter {
  public Message ToEnvelope<T>( IMessage message, string? requestId = null ) where T : IMessage;

  public T FromEnvelope<T>( Message envelope ) where T : IMessage;
}