using System.Text.Json;
using Nuke.Common.Tools.Docker;

namespace Drift.Cli.E2ETests.Container;

internal abstract class DriftContainerImageFixture {
  protected string ImageTag {
    get;
  } = "localhost:5000/drift:dev";

  protected JsonDocument Inspect() {
    var output = DockerTasks.DockerImageInspect( options => options.SetImages( ImageTag ) );

    var jsonText = string.Join( Environment.NewLine, output.Select( o => o.Text ) );

    return JsonDocument.Parse( jsonText );
  }
}