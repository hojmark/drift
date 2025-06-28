using Drift.Spec.Schema;
using Drift.Spec.Schema.Generation;
using SpecVersion = Drift.Spec.Schema.SpecVersion;

const SpecVersion version = SpecVersion.V1_preview;

try {
  var filePath = Path.GetFullPath( Path.Combine( "embedded_resources/schemas", version.ToFileName() ) );
  var serialized = SchemaGenerator.Generate( version );
  File.WriteAllText( filePath, serialized );
  Console.WriteLine( $"Generated schema for version {version} ({filePath})" );
  Environment.Exit( 0 );
}
catch ( Exception ex ) {
  Console.WriteLine( $"Failed to generate schema for version {version}: {ex.Message}" );
  Environment.Exit( 1 );
}