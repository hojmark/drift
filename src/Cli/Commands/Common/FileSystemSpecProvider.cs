using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Normal;
using Drift.Domain;
using Drift.Spec.Schema;
using Drift.Spec.Serialization;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Common;

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
      throw;
    }

    if ( filePath != null ) {
      output.Log.LogDebug( "Using network spec: {Spec}", filePath );
      output.Normal.Write( "Using network spec " );
      output.Normal.Write( $"{filePath}  ", ConsoleColor.Cyan );

      var specFileContents = await new StreamReader( filePath.Open( FileMode.Open, FileAccess.Read, FileShare.Read ) )
        .ReadToEndAsync();
      var valid = SpecValidator.Validate( specFileContents, SpecVersion.V1_preview ).IsValid;

      output.Normal.WriteLineValidity( valid );

      //output.Normal.WriteLine();

      spec = YamlConverter.Deserialize( filePath! );
    }

    return spec;
  }
}