using System.Data;

namespace Drift.Domain.NeoProgress;

public record ProgressNode<T2> : ProgressNode {
  private T2? _data;

  public ProgressNode( Action<ProgressNode>? onProgress ) : base( onProgress ) {
  }

  public void SetData( T2 value ) {
    _data = value ?? throw new ArgumentNullException( nameof(value) );
    //_onProgress?.Invoke();
  }

  public T2 GetData( ContextKey<T2> key ) {
    return _data;
  }
}

public record ProgressNode {
  private uint _progress;
  private readonly ProgressNode? _parent;
  private readonly Action<ProgressNode>? _onProgress;

  public ProgressNode( Action<ProgressNode>? onProgress ) {
    _onProgress = onProgress;
  }

  protected ProgressNode( ProgressNode parent ) {
    _parent = parent;
  }

  public required Path Path {
    get;
    init;
  }

  public uint Weight {
    get;
    init;
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
        Update();
      }
    }
  }

  private void Update() {
    if ( _parent == null ) {
      _onProgress?.Invoke( this );
    }
    else {
      _parent.Update();
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

  public ProgressNode AddChild( Path path, uint weight = 1 ) {
    var child = new ProgressNode( _onProgress ) { Path = path, Weight = weight };
    Children.Add( child );
    return child;
  }

  public ProgressNode? GetChild( Path path ) {
    if ( Path == path ) return this;
    return Children.SelectMany( c => new[] { c.GetChild( path ) } ).FirstOrDefault( n => n != null );
  }

  public ProgressNode GetOrCreateChild( Path path, uint weight = 1 ) {
    var node = GetChild( path );
    return node ?? AddChild( path, weight );
  }

  public void Complete() {
    if ( Children.Count > 0 ) {
      throw new InvalidOperationException( "Cannot complete node with children" );
    }

    _progress = 100;
    Update();
  }

  public void AssertComplete() {
    // TODO modes: ignore, telemetry/warning, throw
    if ( TotalProgress < 100 ) {
      throw new InvalidOperationException( "Node is not complete" );
    }
  }

  // --------------------------

  private readonly Dictionary<string, object> _context = new();


  public void SetContext<T>( ContextKey<T> key, T value ) {
    _context[key.Name] = value ?? throw new ArgumentNullException( nameof(value) );
    Update();
  }

  public T? GetContext<T>( ContextKey<T> key ) {
    return _context.TryGetValue( key.Name, out var value ) && value is T typed ? typed : default;
  }

  public bool TryGetContext<T>( ContextKey<T> key, out T value ) {
    if ( _context.TryGetValue( key.Name, out var obj ) && obj is T typed ) {
      value = typed;
      return true;
    }

    value = default!;
    return false;
  }
}