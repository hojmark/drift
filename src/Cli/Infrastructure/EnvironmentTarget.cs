namespace Drift.Cli.Infrastructure;

internal enum EnvironmentTargetKind {
  Local,
  Server
}

internal sealed record EnvironmentTarget {
  private EnvironmentTarget( EnvironmentTargetKind kind, Uri? serverAddress ) {
    Kind = kind;
    ServerAddress = serverAddress;
  }

  public EnvironmentTargetKind Kind {
    get;
  }

  public Uri? ServerAddress {
    get;
  }

  public static EnvironmentTarget Local {
    get;
  } = new(EnvironmentTargetKind.Local, null);

  public static EnvironmentTarget ForServer( Uri address ) {
    return new EnvironmentTarget( EnvironmentTargetKind.Server, address );
  }
}