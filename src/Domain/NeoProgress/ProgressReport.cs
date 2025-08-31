namespace Drift.Domain.NeoProgress;

public class ProgressReport {
  public ProgressNode Root {
    get;
    init;
  }

  public uint Progress => Root.TotalProgress;
}