using Drift.Cli.Output.Abstractions;
using Drift.Domain;
using Drift.Spec.Schema;
using Drift.Spec.Serialization;
using Drift.Spec.Validation;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Global;

public static class SpecFileDeserializer {
  /// <summary>
  /// 
  /// </summary>
  /// <param name="specFile"></param>
  /// <param name="output"></param>
  /// <returns></returns>
  /// <exception cref="FileNotFoundException">If <paramref name="specFile"/> is not null and the file could not be found. Does not throw if a file was found using <paramref name="specFile"/> or if it was found using conventional names.</exception>
  internal static Inventory? Deserialize( FileInfo? specFile, IOutputManager output ) {
    Inventory? spec = null;

    if ( specFile == null ) {
      output.Log.LogDebug( "No network spec provided" );
      output.Normal.WriteLineVerbose( "No network spec provided" );
    }

    FileInfo? filePath;
    try {
      filePath = new SpecFileResolver( output, specFile?.DirectoryName ?? Directory.GetCurrentDirectory() )
        .Resolve( specFile?.Name, throwsOnNotFound: specFile != null );
    }
    catch ( FileNotFoundException exception ) {
      output.Log.LogError( exception, "Network spec not found: {SpecPath}", specFile?.FullName );
      output.Normal.WriteLineError( exception.Message );
      throw;
    }

    if ( filePath != null ) {
      output.Log.LogDebug( "Using network spec: {Spec}", filePath );
      output.Normal.WriteLine( "Using network spec" );
      output.Normal.Write( 1, $"{filePath}  ", ConsoleColor.Cyan );

      var specFileContents = new StreamReader( filePath.Open( FileMode.Open, FileAccess.Read, FileShare.Read ) );
      var valid = SpecValidator.Validate( specFileContents.ReadToEnd(), SpecVersion.V1_preview ).IsValid;

      output.Normal.WriteLineVerbose();
      output.Normal.WriteLine(
        valid ? "✔ Valid" : "✖ Validation errors",
        valid ? ConsoleColor.Green : ConsoleColor.Red
      );

      output.Normal.WriteLine();

      spec = YamlConverter.Deserialize( filePath! );
    }

    return spec;
  }
}