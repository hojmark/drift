using Drift.Agent.Host.Schema;
using Microsoft.Extensions.Configuration;

namespace Drift.Agent.Host.Tests;

internal sealed class AgentOptionsTests {
  [Test]
  public void EmbeddedSchemaIsUpdated() {
    Assert.That(
      AgentOptionsSchema.Generate(),
      Is.EqualTo( AgentOptionsSchema.AsText() ),
      "Embedded schema is not up to date. Run: dotnet nuke GenerateHostConfigSchemas"
    );
  }

  [Test]
  public void Bind_Empty_UsesDefaults() {
    var options = AgentOptions.Bind( new ConfigurationBuilder().Build() );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( options.Port, Is.EqualTo( AgentOptions.DefaultPort ) );
      Assert.That( options.Listen, Is.True );
      Assert.That( options.RequestLogging.Enabled, Is.False );
    }
  }

  [Test]
  public void Bind_UnknownKey_Throws() {
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection( [new KeyValuePair<string, string?>( "AgentPort", "1234" )] )
      .Build();

    Assert.Throws<InvalidOperationException>( () => AgentOptions.Bind( configuration ) );
  }
}