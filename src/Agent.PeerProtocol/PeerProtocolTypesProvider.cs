using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol;

internal sealed class PeerProtocolTypesProvider : IPeerMessageTypesProvider {
  internal static readonly Dictionary<string, JsonTypeInfo> Map = new();

  public Dictionary<string, JsonTypeInfo> Get() {
    return Map;
  }
}