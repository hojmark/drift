using System.Reflection;

namespace Drift.Common.EmbeddedResources;

public abstract class EmbeddedResourceProviderBase {
  private const string ResourcePrefix = "embedded_resources";

  protected abstract Assembly ResourceAssembly {
    get;
  }

  // TODO NUKE style paths?
  internal Stream GetStream( string path ) {
    var resolvedPath = ConvertPath( path );

    if ( Exist( resolvedPath ) ) {
      return ResourceAssembly.GetManifestResourceStream( resolvedPath ) ??
             throw new Exception( "Could not get stream for resource" );
    }

    var names = ResourceAssembly.GetManifestResourceNames();
    var available = names.Length == 0 ? "(none)" : string.Join( "\n", names.Select( n => "- " + n ) );

    throw new Exception( "Resource does not exist: " + path + "\n" +
                         "Resolved assembly path: " + resolvedPath + "\n" +
                         "Available resources:\n" +
                         available
    );
  }

  private bool Exist( string resolvedPath ) {
    var resourcesAvailable = ResourceAssembly.GetManifestResourceNames() ??
                             throw new Exception( "Could not determine assembly manifest resources" );

    return resourcesAvailable.Contains( resolvedPath );
  }

  private string ConvertPath( string path ) {
    // Assumes this class is located in the root namespace
    var rootNamespace = ResourceAssembly.GetName().Name
                        ?? throw new Exception( "Could not determine root namespace" );

    return $"{rootNamespace}.{ResourcePrefix}.{path.Replace( "/", "." )}";
  }
}