using Semver;

namespace Drift.Build.Utilities.ContainerImage;

// TODO DUPLICATE: move to shared project
public record ImageReference {
  public required ContainerRegistry Host {
    get;
    init;
  }

  public string? Namespace {
    get;
    init;
  }

  public required string Repository {
    get;
    init;
  }

  public required Tag Tag {
    get;
    init;
  }

  public static ImageReference Localhost( string repository, SemVersion version ) {
    return Localhost( repository, Tag.Version( version ) );
  }

  public static ImageReference Localhost( string repository, Tag tag ) {
    ValidateOrThrow( repository );

    return new ImageReference { Host = LocalhostRegistry.Instance, Repository = repository, Tag = tag };
  }

  public static ImageReference DockerIo( string @namespace, string repository, SemVersion version ) {
    return DockerIo( @namespace, repository, Tag.Version( version ) );
  }

  public static ImageReference DockerIo( string @namespace, string repository, Tag tag ) {
    ValidateOrThrow( @namespace, repository );

    return new ImageReference {
      Host = DockerIoRegistry.Instance, Namespace = @namespace, Repository = repository, Tag = tag
    };
  }

  public override string ToString() {
    var @namespace = Namespace == null ? string.Empty : $"{Namespace}/";
    var name = $"{@namespace}{Repository}";

    return $"{Host}/{name}:{Tag}";
  }

  private static void ValidateOrThrow( string @namespace, string repository ) {
    if ( @namespace.Contains( '/' ) ) {
      throw new ArgumentException( "Contains /", nameof(@namespace) );
    }

    if ( @namespace.Trim().Length != @namespace.Length ) {
      throw new ArgumentException( "Contains whitespace", nameof(@namespace) );
    }

    if ( string.IsNullOrWhiteSpace( @namespace ) ) {
      throw new ArgumentException( "Cannot be null or empty", nameof(@namespace) );
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