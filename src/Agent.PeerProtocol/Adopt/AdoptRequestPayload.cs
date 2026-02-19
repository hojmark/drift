using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Adopt;

internal sealed class AdoptRequestPayload : IPeerRequest<Empty> {
  public static string MessageType => "adopt-request";

  public required string Jwt {
    get;
    set;
  }

  public required string ControllerId {
    get;
    set;
  }

  public static JsonTypeInfo JsonInfo {
    get;
  } = null!;
}