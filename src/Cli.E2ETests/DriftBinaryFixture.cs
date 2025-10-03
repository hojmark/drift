using Drift.Cli.E2ETests.Utils;
using Drift.Common;

namespace Drift.Cli.E2ETests;

[TestFixture]
internal abstract class DriftBinaryFixture {
  protected static ToolWrapper DriftBinary {
    get;
    private set;
  }

  [OneTimeSetUp]
  public void Setup() {
    try {
      var path = EnvironmentVariable.GetOrThrow( EnvVar.DRIFT_BINARY_PATH );
      DriftBinary = new ToolWrapper( path );
    }
    catch ( Exception e ) {
      if ( !EnvironmentVariable.IsCi() ) {
        Assert.Inconclusive( $"{EnvVar.DRIFT_BINARY_PATH} not set" );
      }

      Console.Error.WriteLine( e.StackTrace );
      TestContext.Out.WriteLine( e.StackTrace );
      throw;
    }
  }
}