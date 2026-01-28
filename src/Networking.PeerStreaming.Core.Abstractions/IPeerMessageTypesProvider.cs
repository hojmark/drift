using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public interface IPeerMessageTypesProvider {
  Dictionary<string, JsonTypeInfo> Get();
}