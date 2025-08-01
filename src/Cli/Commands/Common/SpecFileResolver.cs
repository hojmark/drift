using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Common;

internal class SpecFileResolver {
  private readonly string _baseDirectory;
  private readonly IOutputManager _output;

  internal SpecFileResolver( IOutputManager output, string baseDirectory ) {
    _baseDirectory = baseDirectory ?? throw new ArgumentNullException( nameof(baseDirectory) );
    _output = output;
  }

  /// <summary>
  /// Resolves a file path using the following priorities:
  /// <list type="number">
  ///   <item>
  ///     <description>File named <c>{name}</c>. Skipped if name is not provided.</description>
  ///   </item>
  ///   <item>
  ///     <description>File named <c>{name}.spec.yaml</c>. Skipped if name is not provided.</description>
  ///   </item>
  ///   <item>
  ///     <description>File named <c>drift.spec.yaml</c></description>
  ///   </item>
  ///   <item>
  ///     <description>File named <c>*.spec.yaml</c></description>
  ///   </item>
  /// </list>
  /// </summary>
  internal FileInfo? Resolve( string? name, bool? throwsOnNotFound = false ) {
    // Expand ~
    var home = Environment.GetFolderPath( Environment.SpecialFolder.UserProfile );
    if ( name != null && name.StartsWith( "~" + Path.DirectorySeparatorChar ) ) {
      name = Path.Combine( home, name[1..].TrimStart( Path.DirectorySeparatorChar ) );
    }

    if ( name != null && name == "~" ) {
      return ResolveByDefaults( home );
    }

    
    var file = name != null ? ResolveByName( _baseDirectory, name ) : ResolveByDefaults( _baseDirectory );

    if ( file != null ) {
      return file;
    }

    if ( throwsOnNotFound.HasValue && throwsOnNotFound.Value ) {
      throw new FileNotFoundException(
        $"Could not resolve a file for '{name}' in directory '{_baseDirectory}' using conventions: [exact filename] → <name>.spec.yaml → drift.spec.yaml → *.spec.yaml"
      );
    }

    return null;
  }

  private FileInfo? ResolveByName( string directory, string name ) {
    if ( string.IsNullOrWhiteSpace( name ) ) {
      _output.Log.LogError( "Cannot be null or empty." );
      throw new ArgumentException( "Cannot be null or empty.", nameof(name) );
    }

    // Priority 1: Exact filename
    var exactPath = Path.Combine( directory, name );
    if ( File.Exists( exactPath ) ) {
      _output.Log.LogDebug( "Resolved using exact filename: {Path}", exactPath );
      _output.Normal.WriteLineVerbose( $"Resolved using exact filename: {exactPath}" );
      return new FileInfo( exactPath );
    }

    _output.Log.LogTrace( "Exact filename not found: {Path}", exactPath );

    // Priority 2: "{name}.spec.yaml"
    var ymlPath = Path.Combine( directory, name + ".spec.yml" );
    var yamlPath = Path.Combine( directory, name + ".spec.yaml" );
    if ( File.Exists( ymlPath ) ) {
      _output.Log.LogDebug( "Resolved using {{name}}.spec.yml file: {Path}", ymlPath );
      _output.Normal.WriteLineVerbose( $"Resolved using {{name}}.spec.yml file: {ymlPath}" );
      return new FileInfo( ymlPath );
    }

    if ( File.Exists( yamlPath ) ) {
      _output.Log.LogDebug( "Resolved using {{name}}.spec.yml file: {Path}", yamlPath );
      _output.Normal.WriteLineVerbose( $"Resolved using {{name}}.spec.yml file: {yamlPath}" );
      return new FileInfo( yamlPath );
    }

    _output.Log.LogTrace( "{{name}}.spec.[yaml|yml] not found: {Path}", directory + $"{name}.spec.yaml" );

    return null;
  }

  private FileInfo? ResolveByDefaults( string directory ) {
    // TODO consider usefulness of this default?
    // Priority 3: "drift.spec.yaml"
    var driftSpecPath = Path.Combine( directory, "drift.spec.yaml" );
    if ( File.Exists( driftSpecPath ) ) {
      _output.Log.LogInformation( "Resolved using drift.spec.yaml file: {Path}", driftSpecPath );
      _output.Normal.WriteLineVerbose( $"Resolved using drift.spec.yaml file: {driftSpecPath}" );
      return new FileInfo( driftSpecPath );
    }

    _output.Log.LogTrace( "drift.spec.yaml not found: {Path}", driftSpecPath );

    // Priority 4: "*.spec.yaml"
    var specFiles = Directory
      .EnumerateFiles( directory, "*.spec.yaml" )
      .Concat( Directory.EnumerateFiles( directory, "*.spec.yml" ) )
      .ToList();

    if ( specFiles.Any() ) {
      var asteriskSpecPath = specFiles.First();
      _output.Log.LogInformation( "Resolved using *.spec.yaml file: {Path}", asteriskSpecPath );
      _output.Normal.WriteLineVerbose( $"Resolved using *.spec.yaml file: {asteriskSpecPath}" );

      if ( specFiles.Count > 1 ) {
        _output.Log.LogWarning(
          "Found multiple spec files in directory: {SpecFiles}",
          string.Join( ", ", specFiles )
        );
        _output.Normal.WriteLineWarning(
          $"Found multiple spec files in directory: {string.Join( ", ", specFiles )}"
        );
      }

      return new FileInfo( asteriskSpecPath );
    }

    return null;
  }
}