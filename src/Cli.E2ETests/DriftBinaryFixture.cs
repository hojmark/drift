using Drift.Cli.E2ETests.Utils;
using Drift.Common;

namespace Drift.Cli.E2ETests;

[TestFixture]
internal abstract class DriftBinaryFixture {
  protected static ToolWrapper DriftBinary;
  private static string DriftPath;

  [OneTimeSetUp]
  public void Setup() {
    try {
      DriftPath = EnvironmentVariable.GetOrThrow( EnvVar.DRIFT_BINARY_PATH );
    }
    catch ( Exception e ) {
      if ( !EnvironmentVariable.IsCi() ) {
        Assert.Inconclusive( $"{EnvVar.DRIFT_BINARY_PATH} not set" );
      }

      Console.Error.WriteLine( e.StackTrace );
      TestContext.Out.WriteLine( e.StackTrace );
      throw;
    }

    DriftBinary = new ToolWrapper( DriftPath );
  }
}