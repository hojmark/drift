using Nuke.Common.Tools.DotNet;
using Semver;

namespace Versioning;

internal static class SemVersionExtensions {
  /*
   * NUKE build extensions
   */
  internal static DotNetBuildSettings SetVersionProperties(
    this DotNetBuildSettings settings,
    SemVersion version
  ) {
    return settings.SetVersion( version.ToDotNetVersion() )
      .SetAssemblyVersion( version.ToDotNetAssemblyVersion() )
      .SetFileVersion( version.ToDotNetFileVersion() )
      .SetInformationalVersion( version.ToDotNetInformationalVersion() );
  }

  internal static DotNetPublishSettings SetVersionProperties(
    this DotNetPublishSettings settings,
    SemVersion version
  ) {
    return settings.SetVersion( version.ToDotNetVersion() )
      .SetAssemblyVersion( version.ToDotNetAssemblyVersion() )
      .SetFileVersion( version.ToDotNetFileVersion() )
      .SetInformationalVersion( version.ToDotNetInformationalVersion() );
  }

  /*
   * Below methods skips metadata because .NET add the commit hash by itself (somewhere?)
   */
  internal static string ToDotNetAssemblyVersion( this SemVersion version ) {
    // Takes major.minor.patch and optional build number
    return version.WithoutPrereleaseOrMetadata().ToString();
  }

  internal static string ToDotNetVersion( this SemVersion version ) {
    return version.WithoutMetadata().ToString();
  }

  internal static string ToDotNetFileVersion( this SemVersion version ) {
    return version.WithoutPrereleaseOrMetadata().ToString();
  }

  internal static string ToDotNetInformationalVersion( this SemVersion version ) {
    return version.WithoutMetadata().ToString();
  }
}