using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Lint.Rendering;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Renderer;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Lint;

internal class LintCommand : CommandBase<LintParameters, LintCommandHandler> {
  internal LintCommand( IServiceProvider provider ) : base(
    "lint",
    "Validate a network spec", provider
  ) {
  }

  protected override LintParameters CreateParameters( ParseResult result ) {
    return new LintParameters( result );
  }
}

public class LintCommandHandler( IOutputManager output ) : ICommandHandler<LintParameters> {
  public async Task<int> Invoke( LintParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running lint command" );

    FileInfo? filePath;
    try {
      filePath = new SpecFileResolver( output, parameters.SpecFile?.DirectoryName ?? Directory.GetCurrentDirectory() )
        .Resolve( parameters.SpecFile?.Name, throwsOnNotFound: true );
    }
    catch ( FileNotFoundException exception ) {
      output.Log.LogError( exception, "Network spec not found: {SpecPath}", parameters.SpecFile?.FullName );
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    output.Log.LogInformation( "Validating network spec: {Spec}", filePath );
    output.Normal.Write( $"Validating " );
    output.Normal.WriteLine( $"{filePath}  ", ConsoleColor.Cyan );

    var yamlContent = await File.ReadAllTextAsync( filePath.FullName );

    var result = SpecValidator.Validate( yamlContent, Spec.Schema.SpecVersion.V1_preview );

    IRenderer<ValidationResult> renderer =
      parameters.OutputFormat switch {
        OutputFormat.Normal => new NormalRenderer( output.Normal ),
        OutputFormat.Log => new LogRenderer( output.Log ),
        _ => new NullRenderer<ValidationResult>()
      };

    output.Log.LogTrace( "Render scan result using {RendererType}", renderer.GetType().Name );

    renderer.Render( result );

    return result.IsValid ? ExitCodes.Success : ExitCodes.SpecValidationError;
  }
}