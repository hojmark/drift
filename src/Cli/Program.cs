using Drift.Cli;
using Drift.Cli.Abstractions;

try {
  await Bootstrapper.BootstrapAsync();

  var parsed = RootCommandFactory.Create( toConsole: true ).Parse( args );

  return await parsed.InvokeAsync();
}
catch ( Exception e ) {
  // Justification: intentionally using the most basic output form to make sure the error is surfaced, no matter what code fails
#pragma warning disable RS0030
  Console.Error.WriteLine( "---------------------------------------" );
  Console.Error.WriteLine( "FATAL ERROR" );
  Console.Error.WriteLine( "---------------------------------------" );
  Console.ForegroundColor = ConsoleColor.Red;
  Console.Error.WriteLine( e.Message );
  Console.ForegroundColor = ConsoleColor.DarkGray;
  Console.Error.WriteLine( e.StackTrace );
  Console.ResetColor();
#pragma warning restore RS0030

  return ExitCodes.UnknownError;
}