using Drift.Cli;

await Bootstrapper.BootstrapAsync();

var parsed = RootCommandFactory.Create().Parse( args );

return await parsed.InvokeAsync();