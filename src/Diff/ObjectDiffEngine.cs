using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Drift.Diff;

/*public static class DeviceDiffEngine {
  public static List<ObjectDiff<DiffDevice>> Compare(
    List<DiffDevice>? original,
    List<DiffDevice>? updated,
    string path,
    DiffOptions? options = null
  ) {
    var diffs= ObjectDiffEngine.Compare( original, updated, path, options ?? new DiffOptions() );
    diffs.Select( d=>d. )
  }
}*/

public static class ObjectDiffEngine {
  public static List<ObjectDiff> Compare(
    object? original,
    object? updated,
    string path,
    DiffOptions? options = null,
    ILogger? logger = null
  ) {
    if ( options != null ) {
      logger?.LogTrace(
        "Using key selectors for types: {Types}",
        options.ListKeySelectors.Keys.Select( t => t.FullName )
      );
    }

    return Compare( original, updated, path, options ?? new DiffOptions(), [], logger );
  }

  // Note that a <see cref="string"/> is treated like a value type for convenience, though it is technically a reference type.
  private static List<ObjectDiff> Compare(
    object? original,
    object? updated,
    string path,
    DiffOptions options,
    Dictionary<string, int> usedKeySelectorCount,
    ILogger? logger = null
  ) {
    var diffs = new List<ObjectDiff>();

    if ( IsPathIgnored( path, options.IgnorePaths ) ) {
      logger?.LogTrace( "Ignoring path (matched pattern): {Path}", path );
      return diffs;
    }

    if ( original == null && updated == null ) {
      // TODO test. even throw perhaps?
      return diffs;
    }

    if ( original == null ) {
      if ( options.DiffTypes.Contains( DiffType.Added ) ) {
        diffs.Add( new ObjectDiff { PropertyPath = path, DiffType = DiffType.Added, Updated = updated } );
      }

      return diffs;
    }

    if ( updated == null ) {
      if ( options.DiffTypes.Contains( DiffType.Removed ) ) {
        diffs.Add( new ObjectDiff { PropertyPath = path, DiffType = DiffType.Removed, Original = original } );
      }

      return diffs;
    }

    if ( original.GetType() != updated.GetType() ) {
      throw new Exception( "Type mismatch in path '" + path + "': " + original.GetType().FullName + " vs " +
                           updated.GetType().FullName );
    }

    var type = original.GetType();

    // Value types (primitives like int and bool, as well as structs and enums; string is treated like a value type for convenience, though it is technically a reference type
    if ( type.IsValueType || type == typeof(string) ) {
      if ( Equals( original, updated ) ) {
        // TODO add unchanged?
        return diffs;
      }

      if ( options.DiffTypes.Contains( DiffType.Changed ) ) {
        diffs.Add( new ObjectDiff {
          PropertyPath = path, Original = original, Updated = updated, DiffType = DiffType.Changed
        } );
      }

      return diffs;
    }

    // Collections
    if ( typeof(IEnumerable).IsAssignableFrom( type ) && type != typeof(string) ) {
      var originalCollection = ( (IEnumerable) original ).Cast<object>().ToList();
      var updatedCollection = ( (IEnumerable) updated ).Cast<object>().ToList();

      var elementType = type.IsGenericType ? type.GetGenericArguments()[0] : null;

      // Key-based matching
      if ( elementType != null && options.ListKeySelectors.TryGetValue( elementType, out var keySelector ) ) {
        logger?.LogTrace( "Using key selector based matching for {ElementType}", elementType.FullName );
        var origDict = originalCollection.Where( x => x != null ).ToDictionary( x => {
          var selector = keySelector( x );

          string realKey = selector;
          if ( usedKeySelectorCount.TryGetValue( selector, out var count ) ) {
            realKey = selector + "_DUPLICATE_" + count;
            usedKeySelectorCount[selector] = count + 1;
          }

          usedKeySelectorCount.Add( realKey, 1 );

          return realKey;
        } );
        var updDict = updatedCollection.Where( x => x != null ).ToDictionary( x => {
          return keySelector( x );
        } );

        foreach ( var key in origDict.Keys.Union( updDict.Keys ) ) {
          origDict.TryGetValue( key, out var origItem );
          updDict.TryGetValue( key, out var updItem );

          var nestedPath = $"{path}[{key}]";
          diffs.AddRange( Compare( origItem, updItem, nestedPath, options, usedKeySelectorCount ) );
        }
      }
      // Fallback to index-based comparison
      else {
        logger?.LogTrace( "Using index based matching for {ElementType}", elementType?.FullName ?? "null" );

        int max = Math.Max( originalCollection.Count, updatedCollection.Count );

        for ( int i = 0; i < max; i++ ) {
          var origItem = i < originalCollection.Count ? originalCollection[i] : null;
          var updItem = i < updatedCollection.Count ? updatedCollection[i] : null;
          var nestedPath = $"{path}[{i}]";

          diffs.AddRange( Compare( origItem, updItem, nestedPath, options, usedKeySelectorCount ) );
        }
      }

      return diffs;
    }

    // Recurse on properties
    foreach ( var prop in type.GetProperties( BindingFlags.Public | BindingFlags.Instance ) ) {
      var origVal = prop.GetValue( original );
      var updVal = prop.GetValue( updated );
      var propPath = $"{path}.{prop.Name}";

      diffs.AddRange( Compare( origVal, updVal, propPath, options, usedKeySelectorCount ) );
    }

    if ( options.DiffTypes.Contains( DiffType.Unchanged ) ) {
      diffs.AddRange( new ObjectDiff {
        PropertyPath = path, Original = original, Updated = updated, DiffType = DiffType.Unchanged
      } );
    }

    return diffs;
  }

  private static bool IsPathIgnored( string path, HashSet<string> ignorePatterns ) {
    foreach ( var pattern in ignorePatterns ) {
      var regexPattern = "^" + Regex.Escape( pattern )
        .Replace( @"\*", ".*" )
        .Replace( @"\[", "\\[" )
        .Replace( @"\]", "\\]" ) + "$";

      if ( Regex.IsMatch( path, regexPattern ) ) {
        return true;
      }
    }

    return false;
  }
}