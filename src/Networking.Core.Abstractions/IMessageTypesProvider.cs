using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.Core.Abstractions;

public interface IMessageTypesProvider {
  Dictionary<string, JsonTypeInfo> Get();
}