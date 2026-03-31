using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.PeerStreaming.Core.Abstractions;

public class Empty : IPeerResponse {
  private Empty() {
  }

  internal static Empty Instance {
    get;
  } = new();

  public static string MessageType => "empty-response";

  public static JsonTypeInfo JsonInfo => throw new NotSupportedException();
}