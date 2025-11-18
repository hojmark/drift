using System.Reflection;
using System.Text;

namespace Drift.Cli.Infrastructure;

internal static class EmbeddedResourceProvider {
  private static Assembly Assembly => typeof(EmbeddedResourceProvider).Assembly;

  private static string RootNamespace => typeof(DriftCli).Namespace
                                         ?? throw new Exception( "Could not determine root namespace" );

  internal static Stream GetStream( string path ) {
    var resolvedPath = ConvertPath( path );

    if ( Exist( resolvedPath ) ) {
      return Assembly.GetManifestResourceStream( resolvedPath ) ??
             throw new Exception( "Could not get stream for resource" );
    }

    var names = Assembly.GetManifestResourceNames();
    var available = names.Length == 0 ? "(none)" : string.Join( ", ", names );

    throw new Exception( "Resource does not exist: " + path + " (resolved assembly path: " + resolvedPath +
                         ")\nAvailable resources: " + available );
  }

  internal static string ReadText( this Stream stream, Encoding? encoding = null ) {
    return new StreamReader( stream, encoding ?? Encoding.UTF8 ).ReadToEnd();
  }

  private static bool Exist( string resolvedPath ) {
    var resourcesAvailable = Assembly.GetManifestResourceNames() ??
                             throw new Exception( "Could not determine assembly manifest resources" );

    return resourcesAvailable.Contains( resolvedPath );
  }

  private static string ConvertPath( string path ) {
    return $"{RootNamespace}.embedded_resources.{path.Replace( "/", "." )}";
  }
}