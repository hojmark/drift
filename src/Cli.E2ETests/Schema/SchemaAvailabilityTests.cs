namespace Drift.Cli.E2ETests.Schema;

internal sealed class SchemaAvailabilityTests {
  [Explicit( "For now, use schema directly from GitHub repo" )]
  [TestCase( "drift-spec-v1-preview.schema.json" )]
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
  public async Task SchemaIsAvailableAtDocumentedUrl() {
    // Arrange
    var httpClient = new HttpClient();
    var documentedUrl =
      "https://raw.githubusercontent.com/hojmark/drift/refs/heads/main/src/Spec/embedded_resources/schemas/drift-spec-v1-preview.schema.json";

    // Act
    var response = await httpClient.GetAsync( documentedUrl );

    // Assert
    Assert.DoesNotThrow( () => response.EnsureSuccessStatusCode() );
    var content = await response.Content.ReadAsStringAsync()!;
    Assert.That( content, Contains.Substring(
        """
            "version": {
              "type": "string",
              "const": "v1-preview"
            }
        """
      )
    );
  }
}