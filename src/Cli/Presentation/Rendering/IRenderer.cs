namespace Drift.Cli.Presentation.Rendering;

internal interface IRenderer<in T> {
  void Render( T data );
}