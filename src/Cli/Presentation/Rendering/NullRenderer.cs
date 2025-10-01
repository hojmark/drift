namespace Drift.Cli.Presentation.Rendering;

internal class NullRenderer<T> : IRenderer<T> {
  public void Render( T data ) {
    // Null object pattern
  }
}