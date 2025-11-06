namespace Drift.Cli.Settings.FeatureFlags;

public record FeatureFlag( string Name ) {
  // TODO public static readonly FeatureFlag Agent = new("agent");
  public override string ToString() => Name;
}