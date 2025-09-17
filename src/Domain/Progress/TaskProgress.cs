namespace Drift.Domain.Progress;

public class TaskProgress {
  public required string TaskName {
    get;
    init;
  }

  public required uint CompletionPct {
    get;
    init;
  }

  public override string ToString() {
    return $"[{nameof(TaskName)}={TaskName}, {nameof(CompletionPct)}={CompletionPct}]";
  }
}