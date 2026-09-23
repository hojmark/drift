using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Enrollment.Subcommands.Add;

namespace Drift.Cli.Commands.Enrollment;

internal sealed class EnrollmentCommand : ContainerCommandBase {
  internal EnrollmentCommand( IServiceProvider provider ) :
    base( "enrollment", "Manage coordinator agent enrollment" ) {
    Subcommands.Add( new EnrollmentAddCommand( provider ) );
  }
}