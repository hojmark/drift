using Drift.Agent.Host.Schema;
using Drift.Coordinator.Host.Schema;

// Usage: <server|agent> <output-dir>
if ( args.Length != 2 ) {
  await Console.Error.WriteLineAsync( "Usage: <server|agent> <output-dir>" );
  return 1;
}

var role = args[0];
var outputDir = args[1];

(string FileName, Func<string> Generate)? schema = role switch {
  "server" => ( CoordinatorOptionsSchema.FileName, CoordinatorOptionsSchema.Generate ),
  "agent" => ( AgentOptionsSchema.FileName, AgentOptionsSchema.Generate ),
  _ => null
};

if ( schema is null ) {
  await Console.Error.WriteLineAsync( $"Unknown role: {role}" );
  return 1;
}

try {
  var filePath = Path.GetFullPath( Path.Combine( outputDir, schema.Value.FileName ) );
  await File.WriteAllTextAsync( filePath, schema.Value.Generate() );
  Console.ForegroundColor = ConsoleColor.Green;
  Console.WriteLine( $"✓ Generated {role} configuration schema ({filePath})" );
  return 0;
}
catch ( Exception ex ) {
  Console.ForegroundColor = ConsoleColor.Red;
  Console.WriteLine( $"✗ Failed to generate {role} configuration schema: {ex.Message}" );
  return 1;
}