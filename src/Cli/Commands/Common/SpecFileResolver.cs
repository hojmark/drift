using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Common;

internal class SpecFileResolver {
  private readonly string _baseDirectory;
  private readonly IOutputManager _output;
  private readonly ILogOutput _logger;

  internal SpecFileResolver( IOutputManager output, string baseDirectory ) {
    _baseDirectory = baseDirectory ?? throw new ArgumentNullException( nameof(baseDirectory) );
    _output = output;
    _logger = output.Log;
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
    var file = name != null ? ResolveByName( name ) : ResolveByDefaults();

    if ( file != null ) {
      return file;
    }

    if ( throwsOnNotFound.HasValue && throwsOnNotFound.Value ) {
      throw new FileNotFoundException(
        $"Could not resolve a file for '{name}' in directory '{_baseDirectory}' using conventions: {{exact filename}} → {{name}}.spec.yaml → drift.spec.yaml → *.spec.yaml"
      );
    }

    return null;
  }

  private FileInfo? ResolveByName( string name ) {
    if ( string.IsNullOrWhiteSpace( name ) ) {
      _logger.LogError( "Cannot be null or empty." );
      throw new ArgumentException( "Cannot be null or empty.", nameof(name) );
    }

    // Expand
    var expandedName = Path.GetFullPath( name );

    // Priority 1: Exact filename
    var exactPath = Path.Combine( _baseDirectory, name );
    if ( File.Exists( exactPath ) ) {
      _logger.LogDebug( "Resolved using exact filename: {Path}", exactPath );
      _output.Normal.WriteLineVerbose( $"Resolved using exact filename: {exactPath}" );
      return new FileInfo( exactPath );
    }

    _logger.LogTrace( "Exact filename not found: {Path}", exactPath );

    // Priority 2: "{name}.spec.yaml"
    var ymlPath = Path.Combine( _baseDirectory, name + ".spec.yml" );
    var yamlPath = Path.Combine( _baseDirectory, name + ".spec.yaml" );
    if ( File.Exists( ymlPath ) ) {
      _logger.LogDebug( "Resolved using {{name}}.spec.yaml file: {Path}", ymlPath );
      _output.Normal.WriteLineVerbose( $"Resolved using {{name}}.spec.yaml file: {ymlPath}" );
      return new FileInfo( ymlPath );
    }

    if ( File.Exists( yamlPath ) ) {
      _logger.LogDebug( "Resolved using {{name}}.spec.yaml file: {Path}", yamlPath );
      _output.Normal.WriteLineVerbose( $"Resolved using {{name}}.spec.yaml file: {yamlPath}" );
      return new FileInfo( yamlPath );
    }

    _logger.LogTrace( "{{name}}.spec.yaml not found: {Path}", _baseDirectory + $"{name}.spec.yaml" );

    return null;
  }

  private FileInfo? ResolveByDefaults() {
    // TODO consider usefulness of this default?
    // Priority 3: "drift.spec.yaml"
    var driftSpecPath = Path.Combine( _baseDirectory, "drift.spec.yaml" );
    if ( File.Exists( driftSpecPath ) ) {
      _logger.LogInformation( "Resolved using drift.spec.yaml file: {Path}", driftSpecPath );
      _output.Normal.WriteLineVerbose( $"Resolved using drift.spec.yaml file: {driftSpecPath}" );
      return new FileInfo( driftSpecPath );
    }

    _logger.LogTrace( "drift.spec.yaml not found: {Path}", driftSpecPath );

    // Priority 4: "*.spec.yaml"
    var specFiles = Directory
      .EnumerateFiles( _baseDirectory, "*.spec.yaml" )
      .Concat( Directory.EnumerateFiles( _baseDirectory, "*.spec.yml" ) )
      .ToList();

    if ( specFiles.Any() ) {
      var asteriskSpecPath = specFiles.First();
      _logger.LogInformation( "Resolved using *.spec.yaml file: {Path}", asteriskSpecPath );
      _output.Normal.WriteLineVerbose( $"Resolved using *.spec.yaml file: {asteriskSpecPath}" );

      if ( specFiles.Count > 1 ) {
        _logger.LogWarning(
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