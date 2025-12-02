using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol;

public class NullResponse : IPeerResponseMessage {
  public static string MessageType {
    get;
  }

  public static JsonTypeInfo JsonInfo {
    get;
  }
}