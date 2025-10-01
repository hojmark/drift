using Drift.EnvironmentConfig;

namespace Drift.Diff;

public class ObjectDiff {
  public required string PropertyPath {
    get;
    init;
  }

  public required DiffType DiffType {
    get;
    init;
  }

  public object? Original {
    get;
    init;
  }

  public object? Updated {
    get;
    init;
  }

  public override string ToString() {
    return PropertyPath + ":\n" +
           JsonConverter.Serialize( Original ?? "[none]" ) +
           "\n-->\n" +
           JsonConverter.Serialize( Updated ?? "[none]" );
  }
}