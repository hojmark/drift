using System.CommandLine;
using Drift.Cli.Commands.Common.Commands;

namespace Drift.Cli.Commands.Status;

internal sealed class StatusCommand : CommandBase<StatusParameters, StatusCommandHandler> {
  internal StatusCommand( IServiceProvider provider ) : base(
    "status",
    "Show Drift status",
    provider,
    includeSpecArgument: false 
  ) {
  }

  protected override StatusParameters CreateParameters( ParseResult result ) {
    return new StatusParameters( result );
  }
}