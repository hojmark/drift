using Drift.Common.EmbeddedResources;
using Drift.Common.Schemas;

namespace Drift.Agent.Host.Schema;

/// <summary>
/// JSON schema for the agent configuration file (<see cref="AgentOptions"/>).
/// </summary>
public static class AgentOptionsSchema {
  public const string FileName = "drift-agent-v1-preview.schema.json";

  public static string Generate() => JsonSchemaGenerator.Generate<AgentOptions>( FileName );

  internal static string AsText() => EmbeddedResourceProvider.GetStream( $"schemas/{FileName}" ).ReadText();
}