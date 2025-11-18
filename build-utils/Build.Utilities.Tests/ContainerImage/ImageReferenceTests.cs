using Drift.Build.Utilities.ContainerImage;
using Semver;

namespace Drift.Build.Utilities.Tests.ContainerImage;

internal sealed class ImageReferenceTests {
  public static IEnumerable<(ImageReference, string)> SuccessTestCases {
    get {
      yield return (
        ImageReference.Localhost( "drift", Tag.Latest ),
        "localhost:5000/drift:latest"
      );
      yield return (
        ImageReference.DockerIo( "hojmark", "drift", Tag.Version( new SemVersion( 1, 21, 1 ) ) ),
        "docker.io/hojmark/drift:1.21.1"
      );
      yield return (
        ImageReference.Localhost( "drift", Tag.Version( new SemVersion( 2, 0, 0 ) ) ),
        "localhost:5000/drift:2.0.0"
      );
      yield return (
        ImageReference.Localhost( "drift", Tag.Dev ), "localhost:5000/drift:dev"
      );
    }
  }

  public static IEnumerable<Lazy<ImageReference>> FailureTestCases {
    get {
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "drift", string.Empty, Tag.Version( new SemVersion( 1, 21, 1 ) ) ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( string.Empty, "drift", Tag.Version( new SemVersion( 2, 0, 0 ) ) ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "hojmark", string.Empty, Tag.Latest ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "hojmark", " ", Tag.Dev ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "hojmark", "/", Tag.Dev ) );
    }
  }

  [Test]
  [MethodDataSource( nameof(SuccessTestCases) )]
  public async Task SerializationSuccessTest( ImageReference reference, string expected ) {
    await Assert.That( reference.ToString() ).IsEqualTo( expected );
  }

  [Test]
  [MethodDataSource( nameof(FailureTestCases) )]
  public void SerializationFailureTest( Lazy<ImageReference> reference ) {
    Assert.Throws<ArgumentException>( () => _ = reference.Value );
  }
}