using Drift.Cli.Output.Abstractions;
using Drift.Cli.Renderer;
using Drift.Spec.Validation;

namespace Drift.Cli.Commands.Lint.Rendering;

internal class NormalRenderer( INormalOutput output ) : IRenderer<ValidationResult> {
  public void Render( ValidationResult result ) {
    if ( result.IsValid ) {
      output.WriteLine( "✅ Spec is valid" );
    }
    else {
      output.WriteLineError( "❌ Spec is invalid" );
      foreach ( var error in result.Errors ) {
        output.WriteLineError( $"• {error}" );
      }
    }
  }
}