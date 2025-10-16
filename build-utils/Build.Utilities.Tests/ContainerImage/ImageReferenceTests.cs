using System.Collections;
using Drift.Build.Utilities.ContainerImage;
using Semver;

namespace Drift.Build.Utilities.Tests.ContainerImage;

internal class ImageReferenceTests {
  public static IEnumerable SuccessTestCases {
    get {
      yield return new object[] {
        ImageReference.Localhost( "drift", LatestVersion.Instance ), "localhost:5000/drift:latest"
      };
      yield return new object[] {
        ImageReference.DockerIo( "hojmark", "drift", new SemanticVersion( new SemVersion( 1, 21, 1 ) ) ),
        "docker.io/hojmark/drift:1.21.1"
      };
      yield return new object[] {
        ImageReference.Localhost( "drift", new SemanticVersion( new SemVersion( 2, 0, 0 ) ) ),
        "localhost:5000/drift:2.0.0"
      };
      yield return new object[] {
        ImageReference.Localhost( "drift", DevVersion.Instance ), "localhost:5000/drift:dev"
      };
    }
  }

  public static IEnumerable FailureTestCases {
    get {
      yield return () =>
        ImageReference.DockerIo( "drift", string.Empty, new SemanticVersion( new SemVersion( 1, 21, 1 ) ) );
      yield return () =>
        ImageReference.DockerIo( string.Empty, "drift", new SemanticVersion( new SemVersion( 2, 0, 0 ) ) );
      yield return () => ImageReference.DockerIo( "hojmark", string.Empty, LatestVersion.Instance );
      yield return () => ImageReference.DockerIo( "hojmark", " ", DevVersion.Instance );
      yield return () => ImageReference.DockerIo( "hojmark", "/", DevVersion.Instance );
    }
  }

  [Test]
  [TestCaseSource( nameof(SuccessTestCases) )]
  public void SerializationSuccessTest( ImageReference reference, string expected ) {
    Assert.That( reference.ToString(), Is.EqualTo( expected ) );
  }

  [Test]
  [TestCaseSource( nameof(FailureTestCases) )]
  public void SerializationFailureTest( Func<ImageReference> referenceFunc ) {
    Assert.Throws<ArgumentException>( () => referenceFunc().ToString() );
  }
}