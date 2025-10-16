namespace Drift.TestUtilities.ContainerImages;

public abstract record ContainerRegistry;

// TODO DUPLICATE: move to shared project
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