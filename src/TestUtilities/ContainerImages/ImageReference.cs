using Semver;

namespace Drift.TestUtilities.ContainerImages;

// TODO DUPLICATE: move to shared project
public record class ImageReference {
  public ContainerRegistry Host {
    get;
    protected init;
  }

  public virtual string? Namespace {
    get;
    protected init;
  }

  public virtual string Repository {
    get;
    protected init;
  }

  public Tag Tag {
    get;
    protected init;
  }

  public static ImageReference Localhost( string repository, SemVersion semVer ) {
    return Localhost( repository, new SemanticVersion( semVer ) );
  }

  public static ImageReference Localhost( string repository, Tag tag ) {
    ValidateOrThrow( repository );

    return new ImageReference { Host = LocalhostRegistry.Instance, Repository = repository, Tag = tag };
  }

  public static ImageReference DockerIo( string namespaze, string repository, SemVersion version ) {
    return DockerIo( namespaze, repository, new SemanticVersion( version ) );
  }

  public static ImageReference DockerIo( string namespaze, string repository, Tag tag ) {
    ValidateOrThrow( namespaze, repository );

    return new ImageReference {
      Host = DockerIoRegistry.Instance, Namespace = namespaze, Repository = repository, Tag = tag
    };
  }

  public override string ToString() {
    var namespaze = Namespace == null
      ? string.Empty
      : $"{Namespace}/";

    var name = $"{namespaze}{Repository}";

    return $"{Host}/{name}:{Tag}";
  }

  private static (string Namespaze, string Repository) SplitNameOrThrow( string name ) {
    var split = name.Split( '/' );

    if ( split.Length < 2 ) {
      throw new ArgumentException( "Missing / separator", nameof(name) );
    }

    if ( split.Length > 2 ) {
      throw new ArgumentException( "Too many / separators", nameof(name) );
    }

    return ( split[0], split[1] );
  }

  private static void ValidateOrThrow( string namespaze, string repository ) {
    if ( namespaze.Contains( '/' ) ) {
      throw new ArgumentException( "Contains /", nameof(namespaze) );
    }

    if ( namespaze.Trim().Length != namespaze.Length ) {
      throw new ArgumentException( "Contains whitespace", nameof(namespaze) );
    }

    if ( string.IsNullOrWhiteSpace( namespaze ) ) {
      throw new ArgumentException( "Cannot be null or empty", nameof(namespaze) );
    }

    ValidateOrThrow( repository );
  }

  private static void ValidateOrThrow( string repository ) {
    if ( repository.Contains( '/' ) ) {
      throw new ArgumentException( "Contains /", nameof(repository) );
    }

    if ( repository.Trim().Length != repository.Length ) {
      throw new ArgumentException( "Contains whitespace", nameof(repository) );
    }

    if ( string.IsNullOrWhiteSpace( repository ) ) {
      throw new ArgumentException( "Cannot be null or empty", nameof(repository) );
    }
  }

  public ImageReference WithTag( Tag tag ) {
    return this with { Tag = tag };
  }
}