using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Protocol.Scan;

public sealed class ScanSubnetProgressUpdate : IMessage {
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
