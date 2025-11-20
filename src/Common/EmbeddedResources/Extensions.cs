using System.Text;

namespace Drift.Common.EmbeddedResources;

public static class Extensions {
  extension<T>( T ) where T : EmbeddedResourceProviderBase, new() {
    public static Stream GetStream( string path ) => new T().GetStream( path );
  }

  extension( Stream stream ) {
    // Justification: false positive (https://community.sonarsource.com/t/fp-s2325-does-report-on-c-14-extensions/151936)
#pragma warning disable S2325
    public string ReadText( Encoding? encoding = null ) {
#pragma warning restore S2325
      return new StreamReader( stream, encoding ?? Encoding.UTF8 ).ReadToEnd();
    }
  }
}