using Drift.Cli;
using Drift.Cli.Abstractions;

try {
  await Bootstrapper.BootstrapAsync();

  var parsed = RootCommandFactory.Create( toConsole: true ).Parse( args );

  return await parsed.InvokeAsync();
}
catch ( Exception e ) {
  Console.Error.WriteLine( "---------------------------------------" );
  Console.Error.WriteLine( "FATAL ERROR" );
  Console.Error.WriteLine( "---------------------------------------" );
  Console.ForegroundColor = ConsoleColor.Red;
  Console.Error.WriteLine( e.Message );
  Console.ForegroundColor = ConsoleColor.DarkGray;
  Console.Error.WriteLine( e.StackTrace );
  Console.ResetColor();

  return ExitCodes.UnknownError;
}