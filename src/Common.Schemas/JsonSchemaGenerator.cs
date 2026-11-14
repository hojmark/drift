using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Generation;

namespace Drift.Common.Schemas;

/// <summary>
/// Generates a JSON schema for a type.
/// </summary>
public static class JsonSchemaGenerator {
  private static readonly JsonSerializerOptions SerializerOptions = new() {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ) }
  };

  private static readonly SchemaGeneratorConfiguration SchemaConfiguration = new() {
    PropertyNameResolver = PropertyNameResolvers.CamelCase, Generators = { new LowerCaseEnumGenerator() }
  };

  /// <param name="fileName">Schema file name, used to build the schema's published <c>$id</c>.</param>
  public static string Generate<T>( string fileName ) {
    var schema = new JsonSchemaBuilder()
      .Schema( new Uri( "https://json-schema.org/draft/2020-12/schema" ) )
      // TODO Publish and test e2e
      .Id( new Uri( $"https://hojmark.github.io/drift/schemas/{fileName}" ) )
      .FromType<T>( SchemaConfiguration )
      .Build();

    return JsonSerializer.Serialize( schema, SerializerOptions );
  }
}