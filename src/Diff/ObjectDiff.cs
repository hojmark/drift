using Drift.Parsers.EnvironmentJson;

namespace Drift.Diff;

public class ObjectDiff {
  public string PropertyPath {
    get;
    set;
  } = string.Empty;

  public object? Original {
    get;
    set;
  }

  public object? Updated {
    get;
    set;
  }

  public DiffType DiffType {
    get;
    set;
  }

  public override string ToString() {
    return PropertyPath + ":\n" +
           JsonConverter.Serialize( Original ?? "[none]" ) +
           "\n-->\n" +
           JsonConverter.Serialize( Updated ?? "[none]" );
  }
}