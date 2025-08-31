namespace Drift.Domain.NeoProgress;

public class ProgressNode {
  private uint _progress;
  private readonly Action? _onProgress;

  public ProgressNode( Action? onProgress ) {
    _onProgress = onProgress;
  }

  public Path Path {
    get;
    set;
  } = "";

  public uint Weight {
    get;
    set;
  } = 1;

  public uint Progress {
    get => _progress;
    set {
      if ( Children.Any() ) {
        throw new InvalidOperationException( "Cannot set progress on non-leaf node" );
      }

      var shouldUpdate = _progress != value;

      _progress = value;

      if ( shouldUpdate ) {
        _onProgress?.Invoke();
      }
    }
  }

  public uint TotalProgress {
    get {
      if ( Children.Count == 0 ) return Progress;

      var totalWeight = Children.Sum( c => c.Weight );
      if ( totalWeight == 0 ) return 0;

      var weightedProgress = Children.Sum( c => c.TotalProgress * c.Weight );
      return (uint) ( weightedProgress / totalWeight );
    }
  }

  public List<ProgressNode> Children {
    get;
  } = [];

  public IEnumerable<ProgressNode> Descendants {
    get {
      foreach ( var child in Children ) {
        yield return child;
        foreach ( var descendant in child.Descendants ) {
          yield return descendant;
        }
      }
    }
  }

  public ProgressNode Add( Path path ) {
    var child = new ProgressNode( _onProgress ) { Path = path };
    Children.Add( child );
    return child;
  }

  public ProgressNode? Find( string path ) {
    if ( Path == path ) return this;
    return Children.SelectMany( c => new[] { c.Find( path ) } ).FirstOrDefault( n => n != null );
  }

  public ProgressNode GetOrCreate( string path ) {
    var node = Find( path );
    return node ?? Add( path );
  }

  public void Complete() {
    if ( Children.Count > 0 ) {
      throw new InvalidOperationException( "Cannot complete node with children" );
    }

    _progress = 100;
    _onProgress?.Invoke();
  }
}