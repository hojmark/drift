using Spectre.Console;

namespace Drift.Cli.Presentation.Rendering;

internal sealed class DisplayValue( string value ) {
  public string Value => value;

  public string WithoutMarkup => Markup.Remove( value );

  public override string ToString() => value;
}