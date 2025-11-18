using System.Reflection;
using System.Text;

namespace Drift.Spec;

// TODO DUPLICATE: move to shared project
internal static class EmbeddedResourceProvider {
  private const string ResourcePrefix = "embedded_resources";

  private static Assembly Assembly => typeof(EmbeddedResourceProvider).Assembly;

  // TODO NUKE style paths?
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
    // Assumes this class is located in the root namespace
    var projectRootNamespace = typeof(EmbeddedResourceProvider).Namespace ??
                               throw new Exception( "Could not determine root namespace" );

    return $"{projectRootNamespace}.{ResourcePrefix}.{path.Replace( "/", "." )}";
  }
}