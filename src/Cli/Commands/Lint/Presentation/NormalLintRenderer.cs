using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Spec.Validation;

namespace Drift.Cli.Commands.Lint.Presentation;

internal class NormalLintRenderer( INormalOutput output ) : IRenderer<ValidationResult> {
  public void Render( ValidationResult result ) {
    output.WriteLineValidity( result.IsValid );

    if ( !result.IsValid ) {
      foreach ( var error in result.Errors ) {
        output.WriteLineError( $"• {error}" );
      }
    }
  }
}