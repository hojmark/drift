using System.CommandLine;

namespace Drift.Cli.Commands.Common.Commands;

internal class ContainerCommandBase : Command {
  public ContainerCommandBase( string name, string description ) : base( name, description ) {
  }
}