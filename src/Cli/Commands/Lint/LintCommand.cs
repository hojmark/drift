using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Lint.Rendering;
using Drift.Cli.Output;
using Drift.Cli.Renderer;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Lint;

internal class LintCommand : CommandBase<LintParameters> {
  internal LintCommand( OutputManagerFactory outputManagerFactory ) : base(
    "lint",
    "Validate a network spec",
    outputManagerFactory.Create,
    r => new LintParameters( r )
  ) {
  }

  protected override async Task<int> Invoke( CancellationToken cancellationToken, LintParameters parameters ) {
    Output.Log.LogDebug( "Running lint command" );

    FileInfo? filePath;
    try {
      filePath = new SpecFileResolver( Output, parameters.SpecFile?.DirectoryName ?? Directory.GetCurrentDirectory() )
        .Resolve( parameters.SpecFile?.Name, throwsOnNotFound: true );
    }
    catch ( FileNotFoundException exception ) {
      Output.Log.LogError( exception, "Network spec not found: {SpecPath}", parameters.SpecFile?.FullName );
      Output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    Output.Log.LogInformation( "Validating network spec: {Spec}", filePath );
    Output.Normal.Write( $"Validating " );
    Output.Normal.WriteLine( $"{filePath}  ", ConsoleColor.Cyan );

    var yamlContent = await File.ReadAllTextAsync( filePath.FullName );

    var result = SpecValidator.Validate( yamlContent, Spec.Schema.SpecVersion.V1_preview );

    IRenderer<ValidationResult> renderer =
      parameters.OutputFormat switch {
        OutputFormat.Normal => new NormalRenderer( Output.Normal ),
        OutputFormat.Log => new LogRenderer( Output.Log ),
        _ => new NullRenderer<ValidationResult>()
      };

    Output.Log.LogTrace( "Render scan result using {RendererType}", renderer.GetType().Name );

    renderer.Render( result );

    return result.IsValid ? ExitCodes.Success : ExitCodes.ValidationError;
  }
}