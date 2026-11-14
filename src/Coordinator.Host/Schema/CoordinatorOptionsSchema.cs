using Drift.Common.EmbeddedResources;
using Drift.Common.Schemas;

namespace Drift.Coordinator.Host.Schema;

/// <summary>
/// JSON schema for the server configuration file (<see cref="CoordinatorOptions"/>).
/// </summary>
public static class CoordinatorOptionsSchema {
  public const string FileName = "drift-server-v1-preview.schema.json";

  public static string Generate() => JsonSchemaGenerator.Generate<CoordinatorOptions>( FileName );

  internal static string AsText() => EmbeddedResourceProvider.GetStream( $"schemas/{FileName}" ).ReadText();
}