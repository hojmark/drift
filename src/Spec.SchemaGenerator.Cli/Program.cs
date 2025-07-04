using Drift.Spec.Schema;
using Drift.Spec.Schema.Generation;
using SpecVersion = Drift.Spec.Schema.SpecVersion;

const SpecVersion version = SpecVersion.V1_preview;

Console.WriteLine( "Generating schema for version " + version );
Console.WriteLine( "Arguments: " + string.Join( " ", args ) );

var outputDir = args[0];

try {
  //var filePath = Path.GetFullPath( Path.Combine( "embedded_resources/schemas", version.ToJsonSchemaFileName() ) );
  var filePath = Path.GetFullPath( Path.Combine( outputDir, version.ToJsonSchemaFileName() ) );
  var serialized = SchemaGenerator.Generate( version );
  File.WriteAllText( filePath, serialized );
  Console.WriteLine( $"Generated schema for version {version} ({filePath})" );
  Environment.Exit( 0 );
}
catch ( Exception ex ) {
  Console.WriteLine( $"Failed to generate schema for version {version}: {ex.Message}" );
  Environment.Exit( 1 );
}