using System.Diagnostics;
using Humanizer;
using Serilog;

namespace Drift.Build.Utilities;

public sealed class OperationTimer( string operationName ) : IDisposable {
  private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

  public void Dispose() {
    Complete();
  }

  private void Complete() {
    var elapsed = _stopwatch.Elapsed.Humanize( 2 );
    Log.Information( "🏁 {Operation} completed in {Elapsed}", operationName, elapsed );
  }
}