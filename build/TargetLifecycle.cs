using System;
using Humanizer;
using Serilog;

internal class TargetLifecycle( string targetName ) : IDisposable {
  private readonly DateTime _startTime = DateTime.Now;

  private void Complete() {
    var elapsed = ( DateTime.Now - _startTime ).Humanize( 2 );
    Log.Information( "🏁 {Target} completed in {Time}", targetName, elapsed );
  }

  public void Dispose() {
    Complete();
  }
}