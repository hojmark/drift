namespace Drift.Cli.ContainerTests;

internal sealed class LabelsTest : DriftContainerImageFixture {
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
    await Verify( ociAnnotationLabels.Select( s => $"{s.Name}={s.Value}" ) ).UseTypeName( "oci-annotations" );

    await Verify( remainingLabels.Select( s => $"{s.Name}={s.Value}" ) ).UseTypeName( "remaining" );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( ociAnnotationLabels.Select( l => l.Name ), Is.SubsetOf( _ociAnnotationsV1_1_1 ) );
      Assert.That( remainingLabels, Has.All.Matches<(string Name, string Value)>( l => l.Value == string.Empty ) );
    }
  }
}