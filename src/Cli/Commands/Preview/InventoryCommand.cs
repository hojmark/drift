using System.CommandLine;

namespace Drift.Cli.Commands.Preview;

internal class InventoryCommand : Command {
  internal InventoryCommand() : base( "inventory", "Manage your declared inventory" ) {
    Options.Add(
      new Option<FileInfo>( "--spec", "-s" ) { Description = "Path to the spec file (e.g. network.spec.yaml)" } );

    var listCmd =
      new Command( "list", "ls" ) { Description = "List devices, subnets, or other resources defined in the spec." };
    listCmd.Options.Add( new Option<string>( "--devices" ) { Description = "List only devices" } );
    listCmd.Options.Add( new Option<string>( "--subnets" ) { Description = "List only subnets" } );
    listCmd.Options.Add( new Option<string>( "--tags" ) { Description = "List only devices with the specified tag" } );
    Subcommands.Add( listCmd );

    var summaryCmd = new Command( "summary", "Show a high-level summary of the inventory." );
    Subcommands.Add( summaryCmd );

    // What does this mean exactly? How would it be different from the lint command?
    var validateCmd = new Command( "validate", "Validate the inventory." );
    Subcommands.Add( validateCmd );

    var diffCmd = new Command( "diff", "Compare inventory spec with another spec" );
    diffCmd.Options.Add( new Option<FileInfo>( "--against" ) {
      Description = "Path to the spec file to compare against"
    } );
    Subcommands.Add( diffCmd );

    var showCmd =
      new Command( "show", "Show full detail for a specific resource by ID, name, or IP." ); //TODO at least by ID...
    showCmd.Arguments.Add( new Argument<string>( "resource-id"
    ) { Description = "The ID of the resource to show." } ); // Note: resource id != device id
  }
}