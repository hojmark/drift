using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Spec.Subcommands.Apply;

namespace Drift.Cli.Commands.Spec;

internal sealed class SpecCommand : ContainerCommandBase {
  internal SpecCommand( IServiceProvider provider ) : base( "spec", "Manage the coordinator network spec" ) {
    Subcommands.Add( new SpecApplyCommand( provider ) );
  }
}