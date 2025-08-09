using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Normal;
using Drift.Cli.Renderer;
using Drift.Spec.Validation;

namespace Drift.Cli.Commands.Lint.Rendering;

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