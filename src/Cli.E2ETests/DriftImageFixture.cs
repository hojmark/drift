using System.Text.Json;
using Drift.Cli.E2ETests.Abstractions;
using HLabs.ImageReferences;
using Nuke.Common.Tools.Docker;

namespace Drift.Cli.E2ETests;

internal abstract class DriftImageFixture {
  protected static QualifiedImageRef DriftImage {
    get;
    private set;
  }

  [OneTimeSetUp]
  public void Setup() {
    try {
      var reference = EnvironmentVariable.GetOrThrow( EnvVar.DRIFT_CONTAINER_IMAGE_REF );
      try {
        DriftImage = reference.QualifiedImage();
      }
      catch ( Exception e ) {
        Console.Error.WriteLine( $"Failed to canonicalize image reference: {reference}" );
        TestContext.Out.WriteLine( $"Failed to canonicalize image reference: {reference}" );
        throw;
      }
      // DriftImage = new PartialImageRef( Registry.Localhost, new Repository( "drift" ), new Tag( "dev" ) ).Qualify();
    }
    catch ( Exception e ) {
      if ( !TestUtilities.Environment.IsCi() ) {
        Assert.Inconclusive( $"{EnvVar.DRIFT_CONTAINER_IMAGE_REF} not set" );
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