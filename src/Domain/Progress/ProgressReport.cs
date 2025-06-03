namespace Drift.Domain.Progress;

public class ProgressReport {
  public List<TaskProgress> Tasks {
    get;
    init;
  } = [];

  public override string ToString() {
    return string.Join( "\n", Tasks );
  }
}