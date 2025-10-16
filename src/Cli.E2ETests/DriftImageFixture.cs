using System.Text.Json;
using Drift.Cli.E2ETests.Abstractions;
using Drift.TestUtilities.ContainerImages;
using Nuke.Common.Tools.Docker;

namespace Drift.Cli.E2ETests;

internal abstract class DriftImageFixture {
  protected static ImageReference DriftImage {
    get;
    private set;
  }

  [OneTimeSetUp]
  public void Setup() {
    try {
      var tag = EnvironmentVariable.GetOrThrow( EnvVar.DRIFT_CONTAINER_IMAGE_TAG );
      // TODO get from env var
      // DriftImage = ImageReference.Parse( path );
      DriftImage = ImageReference.Localhost( "drift", DevVersion.Instance );
    }
    catch ( Exception e ) {
      if ( !TestUtilities.Environment.IsCi() ) {
        Assert.Inconclusive( $"{EnvVar.DRIFT_CONTAINER_IMAGE_TAG} not set" );
      }

      Console.Error.WriteLine( e.StackTrace );
      TestContext.Out.WriteLine( e.StackTrace );
      throw;
    }
  }

  protected static JsonDocument Inspect() {
    var output = DockerTasks.DockerImageInspect( options => options.SetImages( DriftImage.ToString() ) );

    var jsonText = string.Join( Environment.NewLine, output.Select( o => o.Text ) );

    return JsonDocument.Parse( jsonText );
  }
}