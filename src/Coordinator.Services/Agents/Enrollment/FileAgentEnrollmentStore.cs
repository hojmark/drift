using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Coordinator.Services.State;
using Drift.Serialization.Converters;

namespace Drift.Coordinator.Services.Agents.Enrollment;

internal sealed class FileAgentEnrollmentStore( ICoordinatorDataLocation dataLocation ) : IAgentEnrollmentStore {
  public IReadOnlyCollection<AgentEnrollmentRecord> Load() {
    if ( !File.Exists( dataLocation.AgentEnrollmentFile ) ) {
      return [];
    }

    return JsonSerializer.Deserialize<List<AgentEnrollmentRecord>>(
      File.ReadAllText( dataLocation.AgentEnrollmentFile ),
      FileAgentEnrollmentJsonSerializerContext.Default.ListAgentEnrollmentRecord
    ) ?? [];
  }

  public void Save( IReadOnlyCollection<AgentEnrollmentRecord> records ) {
    dataLocation.EnsureCreated();
    var temporaryFile = dataLocation.AgentEnrollmentFile + ".tmp";
    File.WriteAllText(
      temporaryFile,
      JsonSerializer.Serialize(
        records,
        FileAgentEnrollmentJsonSerializerContext.Default.IReadOnlyCollectionAgentEnrollmentRecord
      )
    );
    File.Move( temporaryFile, dataLocation.AgentEnrollmentFile, true );
  }
}

[JsonSourceGenerationOptions(
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  WriteIndented = true,
  Converters = [typeof(AgentIdConverter)] )]
[JsonSerializable( typeof(List<AgentEnrollmentRecord>) )]
[JsonSerializable( typeof(IReadOnlyCollection<AgentEnrollmentRecord>) )]
internal sealed partial class FileAgentEnrollmentJsonSerializerContext : JsonSerializerContext;