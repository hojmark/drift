using Nuke.Common.Tools.DotNet;
using Semver;

namespace Versioning;

internal static class SemVersionExtensions {
  internal static string ToContainerTag( this SemVersion version ) {
    return version.ToString();
  }

  public static DotNetBuildSettings SetVersionProperties(
    this DotNetBuildSettings settings,
    SemVersion version
  ) {
    return settings.SetVersion( version.ToDotNetVersion() )
      .SetAssemblyVersion( version.ToDotNetAssemblyVersion() )
      .SetFileVersion( version.ToDotNetFileVersion() )
      .SetInformationalVersion( version.ToDotNetInformationalVersion() );
  }

  public static DotNetPublishSettings SetVersionProperties(
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

  public static string ToDotNetAssemblyVersion( this SemVersion version ) {
    // Takes major.minor.patch and optional build number
    return version.WithoutPrereleaseOrMetadata().ToString();
  }

  public static string ToDotNetVersion( this SemVersion version ) {
    return version.WithoutMetadata().ToString();
  }

  public static string ToDotNetFileVersion( this SemVersion version ) {
    return version.WithoutPrereleaseOrMetadata().ToString();
  }

  public static string ToDotNetInformationalVersion( this SemVersion version ) {
    return version.WithoutMetadata().ToString();
  }
}