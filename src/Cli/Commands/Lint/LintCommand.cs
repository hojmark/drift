using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Lint.Presentation;
using Drift.Cli.Presentation.Console;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.SpecFile;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Lint;

internal class LintCommand : CommandBase<LintParameters, LintCommandHandler> {
  internal LintCommand( IServiceProvider provider ) : base(
    "lint",
    "Validate a network spec",
    provider
  ) {
  }

  protected override LintParameters CreateParameters( ParseResult result ) {
    return new LintParameters( result );
  }
}

internal class LintCommandHandler( IOutputManager output ) : ICommandHandler<LintParameters> {
  public async Task<int> Invoke( LintParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running lint command" );

    FileInfo? filePath;
    try {
      filePath = new SpecFilePathResolver(
          output,
          parameters.SpecFile?.DirectoryName ?? Directory.GetCurrentDirectory()
        )
        .Resolve( parameters.SpecFile?.Name, throwsOnNotFound: true );
    }
    catch ( FileNotFoundException exception ) {
      output.Log.LogError( exception, "Network spec not found: {SpecPath}", parameters.SpecFile?.FullName );
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    output.Log.LogInformation( "Validating network spec: {Spec}", filePath );
    output.Normal.Write( "Validating " );
    output.Normal.WriteLine( $"{filePath}  ", ConsoleColor.Cyan );

    var yamlContent = await File.ReadAllTextAsync( filePath!.FullName, cancellationToken );

    var result = SpecValidator.Validate( yamlContent, Spec.Schema.SpecVersion.V1_preview );

    IRenderer<ValidationResult> renderer =
      parameters.OutputFormat switch {
        OutputFormat.Normal => new NormalLintRenderer( output.Normal ),
        OutputFormat.Log => new LogLintRenderer( output.Log ),
        _ => new NullRenderer<ValidationResult>()
      };

    output.Log.LogTrace( "Render scan result using {RendererType}", renderer.GetType().Name );

    renderer.Render( result );

    output.Log.LogDebug( "lint command completed" );

    return result.IsValid
      ? ExitCodes.Success
      : ExitCodes.SpecValidationError;
  }
}