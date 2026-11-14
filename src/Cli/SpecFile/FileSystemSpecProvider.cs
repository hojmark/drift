using System.Text.RegularExpressions;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Domain;
using Drift.Spec.Schema;
using Drift.Spec.Serialization;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace Drift.Cli.SpecFile;

internal class FileSystemSpecProvider( IOutputManager output ) : ISpecFileProvider {
  public async Task<Inventory?> GetDeserializedAsync( FileInfo? specFile ) {
    Inventory? spec = null;

    if ( specFile == null ) {
      output.Log.LogDebug( "No network spec provided" );
      output.Normal.WriteLineVerbose( "No network spec provided" );
    }

    FileInfo? filePath;
    try {
      filePath = new SpecFilePathResolver( output, specFile?.DirectoryName ?? Directory.GetCurrentDirectory() )
        .Resolve( specFile?.Name, throwsOnNotFound: specFile != null );
    }
    catch ( FileNotFoundException exception ) {
      output.Log.LogError( exception, "Network spec not found: {SpecPath}", specFile?.FullName );
      output.Normal.WriteLineError( exception.Message );
      throw new FileNotFoundException(
        "A file with the provided name could not be found",
        specFile?.FullName,
        exception
      );
    }

    if ( filePath != null ) {
      output.Log.LogDebug( "Using network spec: {Spec}", filePath );
      output.Normal.WriteLine( "Using network spec " );
      output.Normal.Write( $"  {filePath}  ", ConsoleColor.Cyan );

      string specFileContents;
      await using ( var fs = filePath.Open( FileMode.Open, FileAccess.Read, FileShare.Read ) )
      using ( var sr = new StreamReader( fs ) ) {
        specFileContents = await sr.ReadToEndAsync();
      }

      var validationResult = SpecValidator.Validate( specFileContents, SpecVersion.V1_preview );

      if ( validationResult.IsValid ) {
        output.Normal.WriteLineSuccess( "Valid" );
      }
      else {
        output.Normal.WriteLineError( "Validation failed" );
        foreach ( var error in validationResult.Errors ) {
          output.Normal.WriteLineError( $"  {error}" );
          output.Log.LogWarning( "Spec validation error: {Error}", error.ToString() );
        }
      }

      // A spec that violates the schema is still used if it can be loaded; only abort if it can't.
      try {
        spec = YamlConverter.Deserialize( specFileContents );
      }
      catch ( YamlException exception ) {
        output.Log.LogError( exception, "Network spec could not be loaded: {SpecPath}", filePath );
        output.Normal.WriteLineError( $"Network spec could not be loaded: {filePath}" );
        throw new InvalidSpecException( $"Network spec could not be loaded: {filePath}", exception );
      }

      spec.Network.Id = GetNetworkId( filePath );

      output.Log.LogDebug( "Network ID: {ID}", spec.Network.Id );
      output.Normal.WriteLineVerbose( $"Network ID: {spec.Network.Id}" );

      output.Normal.WriteLine();
    }

    return spec;
  }

  // HACK define in spec or use file name? if using file name, centralize parsing logic
  internal static NetworkId GetNetworkId( FileInfo file ) {
    var regex = new Regex( @".*[/\\](\S+)\.spec\.(?:yaml|yml)$", RegexOptions.None, TimeSpan.FromSeconds( 1 ) );
    var match = regex.Match( file.ToString() );
    return new NetworkId( match.Groups[1].Value );
  }
}