namespace Drift.Cli.E2ETests.Schemas;

internal sealed class SchemasAvailabilityTests {
  [Explicit( "For now, use schema directly from GitHub repo" )]
  [TestCase( "drift-spec-v1-preview.schema.json" )]
  [TestCase( "drift-settings-v1-preview.schema.json" )]
  public async Task SchemaIsAvailableOnGitHubIo( string schemaName ) {
    // Arrange
    var httpClient = new HttpClient();
    var url = $"https://hojmark.github.io/drift/schemas/{schemaName}";

    // Act
    var response = await httpClient.GetAsync( url );

    // Assert
    Assert.DoesNotThrow( () => response.EnsureSuccessStatusCode() );
    var content = await response.RequestMessage?.Content?.ReadAsStringAsync()!;
    Assert.That( content, Contains.Substring( url ) );
  }

  [Test]
  [TestCase(
    "https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/src/Spec/embedded_resources/schemas/drift-spec-v1-preview.schema.json",
    """
        "version": {
          "type": "string",
          "const": "v1-preview"
        }
    """
  )]
  [TestCase(
    "https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/src/Cli.Settings/embedded_resources/schemas/drift-settings-v1-preview.schema.json",
    """
    "$id": "https://hojmark.github.io/drift/schemas/drift-settings-v1-preview.schema.json",
    """
  )]
  public async Task SchemaIsAvailableAtDocumentedUrl( string documentedUrl, string expectedPartialContent ) {
    // Arrange
    var httpClient = new HttpClient();

    // Act
    var response = await httpClient.GetAsync( documentedUrl );

    // Assert
    Assert.DoesNotThrow( () => response.EnsureSuccessStatusCode() );
    var content = await response.Content.ReadAsStringAsync();
    var normalized = content.Replace( "\r\n", "\n" );
    Assert.That( normalized, Contains.Substring( expectedPartialContent ) );
  }
}