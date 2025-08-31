namespace Drift.Domain.NeoProgress;

public class ProgressBuilder {
  public ProgressBuilder( Action<ProgressNode>? onProgress = null ) {
    Root = new ProgressNode( () => onProgress?.Invoke( Root ) ) { Path = "Root" };
  }

  public readonly ProgressNode Root;


/*public ProgressReportNew Build<TPhase>( TPhase phase, string? message = null ) where TPhase : Enum {
  return new ProgressReportNew { Phase =  phase, Root = Root, Message = message };
}*/
}