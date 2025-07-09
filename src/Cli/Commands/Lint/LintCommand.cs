using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Global;
using Drift.Cli.Commands.Lint.Rendering;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Renderer;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Lint;

internal class LintCommand : Command {
  internal LintCommand( ILoggerFactory loggerFactory ) : base( "lint", "Validate a network spec" ) {
    Options.Add( GlobalParameters.Options.Verbose );

    Options.Add( GlobalParameters.Options.OutputFormatOption );

    Arguments.Add( GlobalParameters.Arguments.SpecOptional );

    this.SetAction( ( result, cancellationToken )  =>
      CommandHandler( ( new ConsoleOutputManagerBinder( loggerFactory ) ).GetBoundValue( result ),
        result.GetValue( GlobalParameters.Arguments.SpecOptional ),
        result.GetValue( GlobalParameters.Options.OutputFormatOption )
      )
    );
  }

  private static async Task<int> CommandHandler(
    IOutputManager output,
    FileInfo? specFile,
    GlobalParameters.OutputFormat outputFormat
  ) {
    output.Log.LogDebug( "Running lint command" );

    FileInfo? filePath;
    try {
      filePath = new SpecFileResolver( output, specFile?.DirectoryName ?? Directory.GetCurrentDirectory() )
        .Resolve( specFile?.Name, throwsOnNotFound: true );
    }
    catch ( FileNotFoundException exception ) {
      output.Log.LogError( exception, "Network spec not found: {SpecPath}", specFile?.FullName );
      output.Normal.WriteLineError( exception.Message );
      return ExitCodes.GeneralError;
    }

    output.Log.LogInformation( "Validating network spec: {Spec}", filePath );
    output.Normal.WriteLine( $"Validating network spec {filePath}" );

    var yamlContent = await File.ReadAllTextAsync( filePath.FullName );

    var result = SpecValidator.Validate( yamlContent, Spec.Schema.SpecVersion.V1_preview );

    IRenderer<ValidationResult> renderer =
      outputFormat switch {
        GlobalParameters.OutputFormat.Normal => new NormalRenderer( output.Normal ),
        GlobalParameters.OutputFormat.Log => new LogRenderer( output.Log ),
        _ => new NullRenderer<ValidationResult>()
      };

    output.Log.LogTrace( "Render scan result using {RendererType}", renderer.GetType().Name );

    renderer.Render( result );

    return result.IsValid ? ExitCodes.Success : ExitCodes.ValidationError;
  }
}