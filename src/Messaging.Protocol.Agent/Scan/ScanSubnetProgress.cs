using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Drift.Networking.Core.Abstractions;

namespace Drift.Messaging.Protocol.Agent.Scan;

public sealed class ScanSubnetProgress : IResponse {
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

  public static JsonTypeInfo JsonInfo => ScanSubnetProgressJsonContext.Default.ScanSubnetProgress;
}

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof(ScanSubnetProgress) )]
internal sealed partial class ScanSubnetProgressJsonContext : JsonSerializerContext;