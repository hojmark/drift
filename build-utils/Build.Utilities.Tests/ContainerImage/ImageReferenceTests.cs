using System.Collections;
using Drift.Build.Utilities.ContainerImage;
using Semver;

namespace Drift.Build.Utilities.Tests.ContainerImage;

internal class ImageReferenceTests {
  public static IEnumerable<(ImageReference, string)> SuccessTestCases {
    get {
      yield return (
        ImageReference.Localhost( "drift", LatestVersion.Instance ),
        "localhost:5000/drift:latest"
      );
      yield return (
        ImageReference.DockerIo( "hojmark", "drift", new SemanticVersion( new SemVersion( 1, 21, 1 ) ) ),
        "docker.io/hojmark/drift:1.21.1"
      );
      yield return (
        ImageReference.Localhost( "drift", new SemanticVersion( new SemVersion( 2, 0, 0 ) ) ),
        "localhost:5000/drift:2.0.0"
      );
      yield return (
        ImageReference.Localhost( "drift", DevVersion.Instance ), "localhost:5000/drift:dev"
      );
    }
  }

  public static IEnumerable<Lazy<ImageReference>> FailureTestCases {
    get {
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "drift", string.Empty, new SemanticVersion( new SemVersion( 1, 21, 1 ) ) ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( string.Empty, "drift", new SemanticVersion( new SemVersion( 2, 0, 0 ) ) ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "hojmark", string.Empty, LatestVersion.Instance ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "hojmark", " ", DevVersion.Instance ) );
      yield return new Lazy<ImageReference>( () =>
        ImageReference.DockerIo( "hojmark", "/", DevVersion.Instance ) );
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