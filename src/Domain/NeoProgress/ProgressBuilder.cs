namespace Drift.Domain.NeoProgress;
/*
public class ProgressBuilder<TRoot> {
  private readonly Func<ProgressNode, TRoot> _factory;

  public ProgressBuilder( Func<ProgressNode, TRoot> factory, Action<ProgressNode>? onProgress = null ) {
    _factory = factory;
    _onProgress = () => onProgress?.Invoke( _rootNode! );
    _rootNode = new ProgressNode( _onProgress ) { Path = "Root" };
  }

  private readonly ProgressNode _rootNode;
  private readonly Action _onProgress;

  public TRoot Build() {
    var root = _factory( _rootNode );
    _onProgress.Invoke(); // Trigger initial progress update i.e. 0%
    return root;
  }
}
*/
public abstract class BaseProgressDefinition {
  /*public ProgressNode Node {
    get;
    init;
  }*/
}