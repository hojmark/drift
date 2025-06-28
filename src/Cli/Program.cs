using System.CommandLine.Parsing;
using Drift.Cli;

await Bootstrapper.BootstrapAsync();

var parser = RootCommandFactory.CreateParser();

return await parser.InvokeAsync( args );