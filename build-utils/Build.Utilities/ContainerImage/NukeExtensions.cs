using HLabs.Containers;
using Nuke.Common.Tools.Docker;

namespace Drift.Build.Utilities.ContainerImage;

public static class NukeExtensions {
  public static DockerBuildSettings SetTag( this DockerBuildSettings settings, ImageReference imageReference ) {
    return settings.SetTag( imageReference.ToString() );
  }

  public static DockerTagSettings SetSourceImage( this DockerTagSettings settings, ImageReference imageReference ) {
    return settings.SetSourceImage( imageReference.ToString() );
  }

  public static DockerTagSettings SetTargetImage( this DockerTagSettings settings, ImageReference imageReference ) {
    return settings.SetTargetImage( imageReference.ToString() );
  }

  public static DockerLoginSettings SetServer( this DockerLoginSettings settings, ContainerRegistry registry ) {
    return settings.SetServer( registry.ToString() );
  }

  public static DockerPushSettings SetName( this DockerPushSettings settings, ImageReference imageReference ) {
    return settings.SetName( imageReference.ToString() );
  }
}