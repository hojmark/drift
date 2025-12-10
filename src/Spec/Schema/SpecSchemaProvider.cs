using System.Collections.Concurrent;
using Drift.Common.EmbeddedResources;
using Json.Schema;

namespace Drift.Spec.Schema;

public static class SpecSchemaProvider {
  private static readonly ConcurrentDictionary<SpecVersion, JsonSchema> SchemaCache = new();

  internal static string GetAsText( SpecVersion version ) {
    return EmbeddedResourceProvider.GetStream( GetPath( version ) ).ReadText();
  }

  internal static JsonSchema Get( SpecVersion version ) {
    return SchemaCache.GetOrAdd( version, v => JsonSchema.FromText( GetAsText( v ) ) );
  }

  private static string GetPath( SpecVersion version ) {
    return $"schemas/{version.ToJsonSchemaFileName()}";
  }
}