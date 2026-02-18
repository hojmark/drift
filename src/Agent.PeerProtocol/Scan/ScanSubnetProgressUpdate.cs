using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Agent.PeerProtocol.Scan;

public sealed class ScanSubnetProgressUpdate : IPeerMessage {
  public static string MessageType => "scan-progress-update";

  public required byte ProgressPercentage {
    get;
    init;
  }

  public required int DevicesFound {
    get;
    init;
  }

  public string Status {
    get;
    init;
  } = string.Empty;

  public static JsonTypeInfo JsonInfo => ScanSubnetProgressUpdateJsonContext.Default.ScanSubnetProgressUpdate;
}

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof(ScanSubnetProgressUpdate) )]
internal sealed partial class ScanSubnetProgressUpdateJsonContext : JsonSerializerContext;
