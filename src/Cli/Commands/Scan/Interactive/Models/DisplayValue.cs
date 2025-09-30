using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive.Models;

internal sealed class DisplayValue( string value ) {
  public string Value => value;

  public string WithoutMarkup => Markup.Remove( value );


  public string PadRight( int totalWidth ) {
    var padLength = totalWidth - WithoutMarkup.Length;

    return padLength > 0
      ? value + new string( ' ', padLength )
      : value;
  }

  public override string ToString() => value;
}