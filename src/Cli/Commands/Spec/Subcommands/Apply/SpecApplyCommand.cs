using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Coordinator.Client;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Spec.Subcommands.Apply;

internal sealed class SpecApplyCommand : CommandBase<SpecApplyParameters, SpecApplyCommandHandler> {
  internal SpecApplyCommand( IServiceProvider provider ) : base(
    "apply",
    "Upload a network spec to the active environment",
    provider,
    includeSpecArgument: false
  ) {
    Arguments.Add( SpecApplyParameters.Arguments.File );
  }

  protected override SpecApplyParameters CreateParameters( ParseResult result ) {
    return new SpecApplyParameters( result );
  }
}

internal sealed record SpecApplyParameters : BaseParameters {
  internal static class Arguments {
    internal static readonly Argument<FileInfo> File = new("file") {
      Description = "The network spec file to upload"
    };
  }

  internal SpecApplyParameters( ParseResult parseResult ) : base( parseResult ) {
    File = parseResult.GetValue( Arguments.File )!;
  }

  internal FileInfo File {
    get;
  }
}

internal sealed class SpecApplyCommandHandler(
  IOutputManager output,
  EnvironmentTargetProvider targetProvider
) : ICommandHandler<SpecApplyParameters> {
  public async Task<int> Invoke( SpecApplyParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'spec apply' command" );

    EnvironmentTarget target;
    try {
      target = targetProvider.Resolve();
    }
    catch ( Exception exception ) when ( exception is FormatException or InvalidOperationException ) {
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    if ( target.Kind == EnvironmentTargetKind.Local ) {
      output.Normal.WriteLineError( "'spec apply' requires an active server environment." );
      output.Normal.WriteLineCTA( "Select one with", "drift env use <name>" );
      return ExitCodes.GeneralError;
    }

    if ( !parameters.File.Exists ) {
      output.Normal.WriteLineError( $"Spec file '{parameters.File.FullName}' was not found." );
      return ExitCodes.GeneralError;
    }

    try {
      var yaml = await File.ReadAllTextAsync( parameters.File.FullName, cancellationToken );
      using var client = ControlApiClient.Create( target.ServerAddress! );
      await client.ApplySpecAsync( yaml, cancellationToken );
      output.Normal.WriteLineSuccess( $"Applied spec '{parameters.File.FullName}'." );
      return ExitCodes.Success;
    }
    catch ( Exception exception ) when ( exception is IOException or HttpRequestException ) {
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }
  }
}