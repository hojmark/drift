using System;
using HLabs.ImageReferences;

namespace Versioning;

internal static class TagExtensions {
  extension( Tag ) {
    public static Tag Dev => new("dev");
    public static Tag StagingRandomGuid() => new($"staging.{Guid.NewGuid():N}");
  }
}