using Drift.Cli.Presentation.Rendering;

namespace Drift.Cli.Commands.Scan.Rendering;

internal abstract class ScanRendererBase : IRenderer<ScanRenderData> {
  // TODO make differences available, so sub classes only need to render
  public abstract void Render( ScanRenderData renderResult );
}