using System;
using System.Diagnostics;
using Humanizer;
using Serilog;

namespace Utilities;

internal sealed class TargetLifecycle( string targetName ) : IDisposable {
  private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

  public void Dispose() {
    Complete();
  }

  private void Complete() {
    var elapsed = _stopwatch.Elapsed.Humanize( 2 );
    Log.Information( "🏁 {Target} completed in {Elapsed}", targetName, elapsed );
  }
}