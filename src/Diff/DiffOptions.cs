using System.Collections.Frozen;

namespace Drift.Diff;

public class DiffOptions {
  private readonly Dictionary<Type, Func<object, string>> _listKeySelectors = new();
  private HashSet<DiffType> _diffTypes = [DiffType.Added, DiffType.Removed, DiffType.Changed];

  public FrozenSet<DiffType> DiffTypes => _diffTypes.ToFrozenSet();

  // Key selectors for list item types i.e., how to identify/destinguish items in a list from each other.
  public FrozenDictionary<Type, Func<object, string>> ListKeySelectors => _listKeySelectors.ToFrozenDictionary();

  public HashSet<string> IgnorePaths {
    get;
    set;
  } = new();

  public DiffOptions SetDiffTypes( params DiffType[] diffTypes ) {
    this._diffTypes = new HashSet<DiffType>( diffTypes );
    return this;
  }

  public DiffOptions SetDiffTypesAll() {
    SetDiffTypes( Enum.GetValues<DiffType>() );
    return this;
  }

  public DiffOptions SetKeySelector<T>( Func<T, string> keySelector ) {
    // TODO use full type name - otherwise type names may clash
    this._listKeySelectors[typeof(T)] = obj => typeof(T).Name + "_" + keySelector( (T) obj );
    return this;
  }
}