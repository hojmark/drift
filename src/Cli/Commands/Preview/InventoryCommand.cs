using System.CommandLine;

namespace Drift.Cli.Commands.Preview;

internal class InventoryCommand : Command {
  internal InventoryCommand() : base( "inventory", "Manage your declared inventory" ) {
    AddOption( new Option<FileInfo>( ["--spec", "-s"], "Path to the spec file (e.g. network.spec.yaml)" ) );

    var listCmd = new Command( "list", "List devices, subnets, or other resources defined in the spec." );
    listCmd.AddAlias( "ls" );
    listCmd.AddOption( new Option<string>( ["--devices"], "List only devices" ) );
    listCmd.AddOption( new Option<string>( ["--subnets"], "List only subnets" ) );
    listCmd.AddOption( new Option<string>( ["--tags"], "List only devices with the specified tag" ) );
    AddCommand( listCmd );

    var summaryCmd = new Command( "summary", "Show a high-level summary of the inventory." );
    AddCommand( summaryCmd );

    // What does this mean exactly? How would it be different from the lint command?
    var validateCmd = new Command( "validate", "Validate the inventory." );
    AddCommand( validateCmd );

    var diffCmd = new Command( "diff", "Compare inventory spec with another spec" );
    diffCmd.AddOption( new Option<FileInfo>( ["--against"], "Path to the spec file to compare against" ) );
    AddCommand( diffCmd );

    var showCmd =
      new Command( "show", "Show full detail for a specific resource by ID, name, or IP." ); //TODO at least by ID...
    showCmd.AddArgument( new Argument<string>( "resource-id",
      "The ID of the resource to show." ) ); // Note: resource id != device id
  }
}