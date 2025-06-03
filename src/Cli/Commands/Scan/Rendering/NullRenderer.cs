using Drift.Cli.Renderer;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class NullRenderer : IRenderer<ScanRenderData> {
  public void Render( ScanRenderData data, ILogger? logger = null ) {
    // Null object pattern
  }
}