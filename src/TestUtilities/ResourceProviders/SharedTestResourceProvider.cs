using System.Reflection;
using System.Text;

namespace Drift.TestUtilities.ResourceProviders;

internal static class SharedTestResourceProvider {
  private static Assembly Assembly => typeof(SharedTestResourceProvider).Assembly;

  public static Stream GetStream( string path ) {
    var resolvedPath = ConvertPath( path );
    if ( !Exist( resolvedPath ) ) {
      throw new Exception( "Resource does not exist: " + path + " (resolved assembly path: " + resolvedPath + ")" );
    }

    return Assembly.GetManifestResourceStream( resolvedPath ) ??
           throw new Exception( "Could not get stream for resource" );
  }

  public static string ReadText( this Stream stream, Encoding? encoding = null ) {
    return new StreamReader( stream, encoding ?? Encoding.UTF8 ).ReadToEnd();
  }

  private static bool Exist( string resolvedPath ) {
    var resourcesAvailable = Assembly.GetManifestResourceNames() ??
                             throw new Exception( "Could not determine assembly manifest resources" );

    return resourcesAvailable.Contains( resolvedPath );
  }

  private static string ConvertPath( string path ) {
    var assemblyName = Assembly.GetName().Name ??
                       throw new Exception( "Could not determine assembly name" );

    return $"{assemblyName}.embedded_resources.{path.Replace( "/", "." )}";
  }
}