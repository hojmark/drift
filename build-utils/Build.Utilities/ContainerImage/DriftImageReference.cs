namespace Drift.Build.Utilities.ContainerImage;

public sealed record class DriftImageReference : ImageReference {
  public override string? Namespace => Host == DockerIoRegistry.Instance ? "hojmark" : null;

  public override string Repository {
    get;
    protected init;
  } = "drift";
}