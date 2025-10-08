using Semver;

namespace Drift.Build.Utilities.ContainerImage;

public abstract record Tag( string TagValue ) {
  public override string ToString() {
    return TagValue;
  }
}

public sealed record SemanticVersion( SemVersion Version ) : Tag( Version.ToString() ) {
  public override string ToString() {
    return TagValue;
  }
}

public sealed record LatestVersion : Tag {
  private LatestVersion() : base( "latest" ) {
  }

  public static readonly LatestVersion Instance = new();

  public override string ToString() {
    return TagValue;
  }
}

public sealed record DevVersion : Tag {
  private DevVersion() : base( "dev" ) {
  }

  public static readonly DevVersion Instance = new();

  public override string ToString() {
    return TagValue;
  }
}