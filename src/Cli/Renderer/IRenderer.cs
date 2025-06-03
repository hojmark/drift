using Microsoft.Extensions.Logging;

namespace Drift.Cli.Renderer;

internal interface IRenderer<in T> {
  void Render( T data, ILogger? logger = null );
}