using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Env.Subcommands;

namespace Drift.Cli.Commands.Env;

internal class EnvCommand : ContainerCommandBase {
  internal EnvCommand( IServiceProvider provider ) : base( "env", "Manage Drift environments" ) {
    /* TODO add status command
       Environment/cluster: main-site [Active]
       Agent: Agent-01 [Running, Healthy]
       Agent: Agent-02 [Stopped, No Auth]
       Agent: Agent-03 [Running, Healthy]

       Other agents:
       Agent: Agent-04 [Running, Pending Adoption]
       Agent: Agent-04 [Running, Unknown]
     */
    Subcommands.Add( new EnvAddCommand( provider ) );
    Subcommands.Add( new EnvListCommand( provider ) );
    Subcommands.Add( new EnvUseCommand( provider ) );
    Subcommands.Add( new EnvRemoveCommand( provider ) );
  }
}