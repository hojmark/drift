using System.Text.RegularExpressions;
using Semver;

namespace Drift.Cli.E2ETests.Container;

internal sealed class LabelsTest : DriftImageFixture {
  private readonly List<string> _ociAnnotationsV1_1_1 = [
    "org.opencontainers.image.created",
    "org.opencontainers.image.authors",
    "org.opencontainers.image.url",
    "org.opencontainers.image.documentation",
    "org.opencontainers.image.source",
    "org.opencontainers.image.version",
    "org.opencontainers.image.revision",
    "org.opencontainers.image.vendor",
    "org.opencontainers.image.licenses",
    "org.opencontainers.image.ref.name",
    "org.opencontainers.image.title",
    "org.opencontainers.image.description",
    "org.opencontainers.image.base.digest",
    "org.opencontainers.image.base.name"
  ];

  private readonly List<string> _ignoredLabels = [
    "io.buildah.version",
  ];

  private readonly List<string> _dynamicLabels = [
    "org.opencontainers.image.created",
    "org.opencontainers.image.version",
    "org.opencontainers.image.revision"
  ];

  [Test]
  public async Task LabelsUseOciAnnotationsTest() {
    // Arrange / Act
    var labels = Inspect().RootElement[0]
      .GetProperty( "Config" )
      .GetProperty( "Labels" )
      .EnumerateObject()
      .Select( o => ( o.Name, Value: o.Value.ToString() ) )
      .ToList();

    var ociAnnotationLabels = labels
      .Where( l => _ociAnnotationsV1_1_1.Contains( l.Name ) )
      .ToList();

    var remainingLabels = labels
      .Except( ociAnnotationLabels )
      .Where( l => !_ignoredLabels.Contains( l.Name ) )
      .ToList();

    // Assert
    // Static OCI annotations
    await Verify( ociAnnotationLabels
      .Where( l => !_dynamicLabels.Contains( l.Name ) )
      .Select( s => $"{s.Name}={s.Value}" )
    ).UseTypeName( "oci-annotations" );

    // Other labels
    await Verify( remainingLabels.Select( s => $"{s.Name}={s.Value}" ) ).UseTypeName( "remaining" );

    // Dynamic OCI annotations
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( ociAnnotationLabels.Select( l => l.Name ), Is.SubsetOf( _ociAnnotationsV1_1_1 ) );
      Assert.That( remainingLabels, Has.All.Matches<(string Name, string Value)>( l => l.Value == string.Empty ) );

      const string revisionLabel = "org.opencontainers.image.revision";
      var commitHash = ociAnnotationLabels.Single( l => l.Name == revisionLabel ).Value;
      Assert.That(
        GitUtils.IsValidGitCommitHash( commitHash ),
        $"Expected {revisionLabel} to be a valid Git commit hash, but it was not: {commitHash}"
      );

      const string versionLabel = "org.opencontainers.image.version";
      var version = ociAnnotationLabels.Single( l => l.Name == versionLabel ).Value;
      Assert.That(
        SemVersion.TryParse( version, out _ ),
        $"Expected {versionLabel} to be a valid semantic version, but it was not: {version}"
      );

      /*
      var created = ociAnnotationLabels.Single( l => l.Name == "org.opencontainers.image.created" ).Value;
      Assert.That(
        DateTime.TryParse( created, CultureInfo.InvariantCulture, out _ ),
        $"Expected org.opencontainers.image.created to be a valid datetime, but it was not: {created}"
      );
      */
    }
  }

  private static class GitUtils {
    // 40-character hexadecimal Git SHA-1 hash
    private static readonly Regex GitCommitHashRegex = new("^[0-9a-f]{40}$", RegexOptions.IgnoreCase);

    public static bool IsValidGitCommitHash( string input ) {
      return GitCommitHashRegex.IsMatch( input );
    }
  }
}