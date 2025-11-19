using System.Text;

namespace Drift.Common.EmbeddedResources;

public static class Extensions {
  extension<T>( T ) where T : EmbeddedResourceProviderBase, new() {
    public static Stream GetStream( string path ) => new T().GetStream( path );
  }

  extension( Stream stream ) {
    public string ReadText( Encoding? encoding = null ) {
      return new StreamReader( stream, encoding ?? Encoding.UTF8 ).ReadToEnd();
    }
  }
}