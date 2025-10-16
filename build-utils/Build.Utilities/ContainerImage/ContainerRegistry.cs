namespace Drift.Build.Utilities.ContainerImage;

public abstract record ContainerRegistry;

public sealed record DockerIoRegistry : ContainerRegistry {
  public static readonly DockerIoRegistry Instance = new();

  private DockerIoRegistry() {
  }

  public override string ToString() {
    return "docker.io";
  }
}

public sealed record LocalhostRegistry : ContainerRegistry {
  public static readonly LocalhostRegistry Instance = new();

  private LocalhostRegistry() {
  }

  public override string ToString() {
    return "localhost:5000";
  }
}