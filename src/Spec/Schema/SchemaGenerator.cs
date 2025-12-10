using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Common.Schemas;
using Json.Schema;
using Json.Schema.Generation;

namespace Drift.Spec.Schema;

public static class SchemaGenerator {
  private static readonly JsonSerializerOptions SerializerOptions = new() {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    Converters = { new JsonStringEnumConverter( JsonNamingPolicy.SnakeCaseLower ) }
  };

  private static readonly SchemaGeneratorConfiguration SchemaConfiguration = new() {
    PropertyNameResolver = PropertyNameResolvers.LowerSnakeCase, Generators = { new LowerCaseEnumGenerator() }
  };

  public static string Generate( SpecVersion version ) {
    return version switch {
      SpecVersion.V1_preview => Generate<Dtos.V1_preview.DriftSpec>( version ),
      _ => throw new ArgumentOutOfRangeException( nameof(version), version, "Unknown spec version" )
    };
  }

  private static string Generate<T>( SpecVersion version ) {
    var schema = new JsonSchemaBuilder()
      // Justification: this never needs to be dynamic
#pragma warning disable S1075
      .Schema( new Uri( "https://json-schema.org/draft/2020-12/schema" ) )
#pragma warning restore S1075
      // TODO Publish and test e2e
      .Id( new Uri( $"https://hojmark.github.io/drift/schemas/{version.ToJsonSchemaFileName()}" ) )
      .FromType<T>( SchemaConfiguration )
      // Create a new empty schema registry to avoid unit test failure when both creating and reading a schema with the same id in the same test process
      // See The https://github.com/json-everything/json-everything/issues/957
      .Build( new BuildOptions { SchemaRegistry = new SchemaRegistry() } );

    return JsonSerializer.Serialize( schema, SerializerOptions );
  }
}