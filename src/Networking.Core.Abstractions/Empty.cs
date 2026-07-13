using System.Text.Json.Serialization.Metadata;

namespace Drift.Networking.Core.Abstractions;

// TODO necessary?
public class Empty : IResponse {
  private Empty() {
  }

  internal static Empty Instance {
    get;
  } = new();

  public static string MessageType => "empty-response";

  public static JsonTypeInfo JsonInfo => throw new NotSupportedException();
}