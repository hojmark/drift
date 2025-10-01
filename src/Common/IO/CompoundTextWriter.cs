using System.Text;

namespace Drift.Common.IO;

public class CompoundTextWriter : TextWriter {
  public List<TextWriter> Writers {
    get;
  } = [];

  public override Encoding Encoding => Encoding.UTF8;

  public override void Write( char value ) {
    foreach ( var w in Writers )
      w.Write( value );
  }

  public override void Write( string? value ) {
    foreach ( var w in Writers )
      w.Write( value );
  }

  public override void WriteLine( string? value ) {
    foreach ( var w in Writers )
      w.WriteLine( value );
  }

  public override void Flush() {
    foreach ( var w in Writers )
      w.Flush();
  }
}