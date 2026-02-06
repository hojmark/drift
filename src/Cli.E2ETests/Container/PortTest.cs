namespace Drift.Cli.E2ETests.Container;

[Platform( "Linux" )]
internal sealed class PortTest : DriftImageFixture {
  [Explicit( "Not implemented yet" )]
  [Test]
  public void AgentPortIsExposed() {
    // Arrange / Act
    var exposedPort = Inspect().RootElement[0]
      .GetProperty( "Config" )
      .GetProperty( "ExposedPorts" )
      .EnumerateObject()
      .Single()
      .Name;

    // Assert
    Assert.That( exposedPort, Is.EqualTo( "45454/tcp" ) );
  }

  // TODO test port is open
}