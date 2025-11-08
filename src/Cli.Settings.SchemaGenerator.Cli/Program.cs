using Drift.Cli.Settings;
using Drift.Cli.Settings.SchemaGenerator.Cli;

const SettingsVersion version = SettingsVersion.V1_preview;

Console.Write( "Generating settings schema for version " );
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine( version );

Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine( "Arguments: " + string.Join( " ", args ) );

var outputDir = args[0];

try {
  // var filePath = Path.GetFullPath( Path.Combine( "embedded_resources/schemas", version.ToJsonSchemaFileName() ) );
  var filePath = Path.GetFullPath( Path.Combine( outputDir, version.ToJsonSchemaFileName() ) );
  var serialized = SchemaGenerator.Generate( version );
  await File.WriteAllTextAsync( filePath, serialized );
  Console.ForegroundColor = ConsoleColor.Green;
  Console.WriteLine( $"✔ Generated settings schema for version {version} ({filePath})" );
  Environment.Exit( 0 );
}
catch ( Exception ex ) {
  Console.ForegroundColor = ConsoleColor.Red;
  Console.WriteLine( $"✗ Failed to generate settings schema for version {version}: {ex.Message}" );
  Environment.Exit( 1 );
}