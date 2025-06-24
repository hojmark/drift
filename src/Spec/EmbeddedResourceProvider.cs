using System.Reflection;
using System.Text;

namespace Drift.Spec;

// TODO DUPLICATE: move to shared project
internal static class EmbeddedResourceProvider {
  private static Assembly Assembly => typeof(EmbeddedResourceProvider).Assembly;
  private const string ResourcePrefix = "embedded_resources";

  //TODO NUKE style paths?
  internal static Stream GetStream( string path ) {
    var resolvedPath = ConvertPath( path );

    if ( Exist( resolvedPath ) ) {
      return Assembly.GetManifestResourceStream( resolvedPath ) ??
             throw new Exception( "Could not get stream for resource" );
    }

    LogAvailableResources();

    throw new Exception( "Resource does not exist: " + path + " (resolved assembly path: " + resolvedPath + ")" );
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

  private static void LogAvailableResources() {
    var names = Assembly.GetManifestResourceNames();

    // Justification: fallback error scenario
#pragma warning disable RS0030
    Console.Error.WriteLine( "Available resources:" );

    if ( names.Length == 0 ) {
      Console.Error.WriteLine( "(none)" );
      return;
    }

    foreach ( var name in names ) {
      Console.Error.WriteLine( name );
    }
#pragma warning restore RS0030
  }
}