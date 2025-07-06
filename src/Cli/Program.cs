using Drift.Cli;

await Bootstrapper.BootstrapAsync();

var parser = RootCommandFactory.Create().Parse( args );

return await parser.InvokeAsync();