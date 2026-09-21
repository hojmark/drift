using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Coordinator.Client;
using Drift.Domain;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Enrollment.Subcommands.Add;

internal sealed class EnrollmentAddCommand : CommandBase<EnrollmentAddParameters, EnrollmentAddCommandHandler> {
  internal EnrollmentAddCommand( IServiceProvider provider ) : base(
    "add",
    "Enroll a declared agent through the coordinator",
    provider,
    includeSpecArgument: false
  ) {
    Arguments.Add( EnrollmentAddParameters.Arguments.Id );
  }

  protected override EnrollmentAddParameters CreateParameters( ParseResult result ) {
    return new EnrollmentAddParameters( result );
  }
}

internal sealed record EnrollmentAddParameters : BaseParameters {
  internal static class Arguments {
    internal static readonly Argument<string> Id = new("id") {
      Description = "The declared agent ID"
    };
  }

  internal EnrollmentAddParameters( ParseResult parseResult ) : base( parseResult ) {
    Id = parseResult.GetValue( Arguments.Id )!;
  }

  internal string Id {
    get;
  }
}

internal sealed class EnrollmentAddCommandHandler(
  IOutputManager output,
  EnvironmentTargetProvider targetProvider
) : ICommandHandler<EnrollmentAddParameters> {
  public async Task<int> Invoke( EnrollmentAddParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'enrollment add' command" );

    EnvironmentTarget target;
    AgentId id;
    try {
      target = targetProvider.Resolve();
      id = AgentId.Parse( parameters.Id, null );
    }
    catch ( Exception exception ) when ( exception is FormatException or InvalidOperationException ) {
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    if ( target.Kind == EnvironmentTargetKind.Local ) {
      output.Normal.WriteLineError( "'enrollment add' requires an active server environment." );
      output.Normal.WriteLineCTA( "Select one with", "drift env use <name>" );
      return ExitCodes.GeneralError;
    }

    try {
      using var client = ControlApiClient.Create( target.ServerAddress! );
      var result = await client.EnrollAgentAsync( id, cancellationToken );
      output.Normal.WriteLineSuccess( $"Enrolled '{result.Id}' ({result.ConnectionStatus})." );
      return ExitCodes.Success;
    }
    catch ( Exception exception ) when ( exception is HttpRequestException or InvalidOperationException ) {
      output.Normal.WriteLineError( $"Unable to enroll agent '{id.Value}': {exception.Message}" );
      return ExitCodes.GeneralError;
    }
  }
}
