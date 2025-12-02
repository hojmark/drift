using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptRequestPayload : IPeerRequestMessage {
  public static string MessageType => "adopt-request";

  public string Jwt {
    get;
    set;
  }

  public string ControllerId {
    get;
    set;
  }

  public static JsonTypeInfo JsonInfo {
    get;
  }
}