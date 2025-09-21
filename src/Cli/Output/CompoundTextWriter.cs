using System.Text;

namespace Drift.Cli.Output;

internal class CompoundTextWriter : TextWriter {
  private readonly TextWriter[] _writers;

  public CompoundTextWriter( params TextWriter[] writers ) {
    _writers = writers;
  }

  public override Encoding Encoding => Encoding.UTF8;

  public override void Write( char value ) {
    foreach ( var w in _writers )
      w.Write( value );
  }

  public override void Write( string? value ) {
    foreach ( var w in _writers )
      w.Write( value );
  }

  public override void WriteLine( string? value ) {
    foreach ( var w in _writers )
      w.WriteLine( value );
  }

  public override void Flush() {
    foreach ( var w in _writers )
      w.Flush();
  }
}