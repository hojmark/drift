using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Common.Schemas;
using Json.Schema;
using Json.Schema.Generation;

namespace Drift.Cli.Settings.SchemaGenerator.Cli;

internal static class SchemaGenerator {
  private static readonly JsonSerializerOptions SerializerOptions = new() {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ) }
  };

  private static readonly SchemaGeneratorConfiguration SchemaConfiguration = new() {
    PropertyNameResolver = PropertyNameResolvers.CamelCase,
    Generators = {
      // TODO Use camelcase?
      new LowerCaseEnumGenerator()
    }
  };

  public static string Generate( SettingsVersion version ) {
    return version switch {
      SettingsVersion.V1_preview => Generate<V1_preview.CliSettings>( version ),
      _ => throw new ArgumentOutOfRangeException( nameof(version), version, "Unknown settings version" )
    };
  }

  private static string Generate<T>( SettingsVersion version ) {
    var schema = new JsonSchemaBuilder()
      // Justification: this never needs to be dynamic
#pragma warning disable S1075
      .Schema( new Uri( "https://json-schema.org/draft/2020-12/schema" ) )
#pragma warning restore S1075
      // TODO Publish and test e2e
      .Id( new Uri( $"https://hojmark.github.io/drift/schemas/{version.ToJsonSchemaFileName()}" ) )
      .FromType<T>( SchemaConfiguration )
      .Build();

    return JsonSerializer.Serialize( schema, SerializerOptions );
  }
}