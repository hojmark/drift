using HLabs.ImageReferences;

namespace Versioning;

internal static class TagExtensions {
  extension( Tag ) {
    public static Tag Dev => new("dev");
  }
}