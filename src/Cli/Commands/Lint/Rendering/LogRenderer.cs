using Drift.Cli.Output.Abstractions;
using Drift.Cli.Renderer;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Lint.Rendering;

internal class LogRenderer( ILogOutput output ) : IRenderer<ValidationResult> {
  public void Render( ValidationResult result ) {
    if ( result.IsValid ) {
      output.LogInformation( "Spec is valid" );
    }
    else {
      output.LogWarning( "Spec is invalid" );
      foreach ( var error in result.Errors ) {
        output.LogError( "{ValidationError}", error.ToString() );
      }
    }
  }
}