namespace Drift.Cli.Renderer;

internal class NullRenderer<T> : IRenderer<T> {
  public void Render( T data ) {
    // Null object pattern
  }
}