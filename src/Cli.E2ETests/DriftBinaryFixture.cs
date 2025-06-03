using Drift.Cli.E2ETests.Utils;
using Drift.Utils;

namespace Drift.Cli.E2ETests;

[TestFixture]
public class DriftBinaryFixture {
  protected static ToolWrapper DriftBinary;
  private static string DriftPath;

  [OneTimeSetUp]
  public void Setup() {
    try {
      DriftPath = EnvironmentVariable.GetOrThrow( EnvVar.DRIFT_BINARY_PATH );
    }
    catch ( Exception e ) {
      Console.Error.WriteLine( e.StackTrace );
      TestContext.Out.WriteLine( e.StackTrace );
      throw;
    }

    DriftBinary = new ToolWrapper( DriftPath );
  }
}