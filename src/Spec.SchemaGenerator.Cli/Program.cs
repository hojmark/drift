using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Spec.Dtos.V1_preview;
using Drift.Spec.SchemaGenerator.Cli;
using Json.Schema;
using Json.Schema.Generation;

var serializerOptions = new JsonSerializerOptions {
  WriteIndented = true,
  PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
  Converters = { new JsonStringEnumConverter( JsonNamingPolicy.SnakeCaseLower ) }
};

// TODO make generic
var type = typeof(DriftSpec);
var directory = "embedded_resources/schemas";
var filePath = "drift-spec-v1-preview.schema.json";

try {
  var genConfig =
    new SchemaGeneratorConfiguration {
      PropertyNameResolver = PropertyNameResolvers.LowerSnakeCase, Generators = { new LowerCaseEnumGenerator() } ,
    }; //TODO yaml?
  var schema = new JsonSchemaBuilder()
    //.FromType<Inventory>( genConfig )
    .Schema( new Uri( "https://json-schema.org/draft/2020-12/schema" ) )
    //Publish and test e2e
    .Id( new Uri( $"https://hojmark.github.io/drift/json-schemas/{filePath}" ) )
    .FromType<DriftSpec>( genConfig )
    .Build();
  var serialized = JsonSerializer.Serialize( schema, serializerOptions );

  /*JsonNode schema = SerializerOptions.GetJsonSchemaAsNode( type );
  var serialized = JsonSerializer.Serialize( schema, SerializerOptions );*/
  var fullPath = Path.GetFullPath( Path.Combine( directory, filePath ) );
  File.WriteAllText( fullPath, serialized );
  Console.WriteLine( $"Generated schema for {type.Name} ({fullPath})" );
  Environment.Exit( 0 );
}
catch ( Exception ex ) {
  Console.WriteLine( $"Failed to generate schema for {type.Name}: {ex.Message}" );
  Environment.Exit( 1 );
}