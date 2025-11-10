namespace Drift.FeatureFlagsDELETE;

public class FeatureFlagService {
  private readonly HashSet<FeatureFlag> _enabledFlags;

  public FeatureFlagService( IEnumerable<FeatureFlag> enabledFlags ) {
    _enabledFlags = new HashSet<FeatureFlag>( enabledFlags );
  }

  public bool IsEnabled( FeatureFlag flag ) => _enabledFlags.Contains( flag );

  public static IEnumerable<HashSet<FeatureFlag>> GetAllCombinations( bool includeEmpty = true ) {
    var flags = Enum.GetValues<FeatureFlag>();
    int totalCombinations = 1 << flags.Length; // 2^n combinations

    for ( int i = 0; i < totalCombinations; i++ ) {
      // Skip the empty set if includeEmpty is false
      if ( !includeEmpty && i == 0 ) {
        continue;
      }

      var combination = new HashSet<FeatureFlag>();
      for ( int j = 0; j < flags.Length; j++ ) {
        if ( ( i & ( 1 << j ) ) != 0 ) {
          combination.Add( flags[j] );
        }
      }

      yield return combination;
    }
  }
}