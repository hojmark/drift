namespace Drift.Domain.NeoProgress;

public class ProgressNodeNew {
  private int _progress;
  private readonly Action? _onProgress;

  public ProgressNodeNew( Action? onProgress ) {
    _onProgress = onProgress;
  }

  public Path Path {
    get;
    set;
  } = "";

  public int Progress {
    get => _progress;
    set {
      if ( Children.Any() ) {
        throw new InvalidOperationException( "Cannot set progress on non-leaf node" );
      }

      _progress = value;
      _onProgress?.Invoke();
    }
  }

  public List<ProgressNodeNew> Children {
    get;
  } = [];

  public ProgressNodeNew Add( Path path ) {
    var child = new ProgressNodeNew( _onProgress ) { Path = path };
    Children.Add( child );
    return child;
  }

  public ProgressNodeNew? Find( string path ) {
    if ( Path == path ) return this;
    return Children.SelectMany( c => new[] { c.Find( path ) } ).FirstOrDefault( n => n != null );
  }

  public int TotalProgress {
    get {
      if ( Children.Count == 0 ) return Progress;

      var totalWeight = Children.Sum( c => c.GetWeight() );
      if ( totalWeight == 0 ) return 0;

      var weightedProgress = Children.Sum( c => c.TotalProgress * c.GetWeight() );
      return weightedProgress / totalWeight;
    }
  }

  public void Complete() {
    if ( Children.Count > 0 ) {
      throw new InvalidOperationException( "Cannot complete node with children" );
    }

    _progress = 100;
    _onProgress?.Invoke();
  }

  // Calculate weight based on number of leaf nodes (actual work items)
  private int GetWeight() {
    if ( Children.Count == 0 ) return 1;
    return Children.Sum( c => c.GetWeight() ); // Sum of children's weights
  }
}

// Simple progress report
public class ProgressReportNew {
  public ProgressNodeNew Root {
    get;
    init;
  }

  public int Progress => Root.TotalProgress;
}

// Scan-specific report
/*public class ScanProgressReportNew : ProgressReportNew<ScanPhase> {
}*/

// Simple builder
public class ProgressBuilderNew {
  public ProgressBuilderNew( Action<ProgressNodeNew>? onProgress = null ) {
    Root = new ProgressNodeNew( () => onProgress?.Invoke( Root ) ) { Path = "Root" };
  }

  public readonly ProgressNodeNew Root;


/*public ProgressReportNew Build<TPhase>( TPhase phase, string? message = null ) where TPhase : Enum {
  return new ProgressReportNew { Phase =  phase, Root = Root, Message = message };
}*/
}