using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Server.Subcommands.Start;

namespace Drift.Cli.Commands.Server;

internal class ServerCommand : ContainerCommandBase {
  internal ServerCommand( IServiceProvider provider ) : base( "server", "Manage the local Drift server" ) {
    Subcommands.Add( new ServerStartCommand( provider ) );
  }
}