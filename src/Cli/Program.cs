using Drift.Cli;

await Bootstrapper.BootstrapAsync();

var parsed = RootCommandFactory.Create( toConsole: true ).Parse( args );

return await parsed.InvokeAsync();