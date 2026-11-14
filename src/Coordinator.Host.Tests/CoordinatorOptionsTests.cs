using Drift.Coordinator.Host.Schema;
using Microsoft.Extensions.Configuration;

namespace Drift.Coordinator.Host.Tests;

internal sealed class CoordinatorOptionsTests {
  [Test]
  public void EmbeddedSchemaIsUpdated() {
    Assert.That(
      CoordinatorOptionsSchema.Generate(),
      Is.EqualTo( CoordinatorOptionsSchema.AsText() ),
      "Embedded schema is not up to date. Run: dotnet nuke GenerateHostConfigSchemas"
    );
  }

  [Test]
  public void Bind_Empty_UsesDefaults() {
    var options = CoordinatorOptions.Bind( Build() );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( options.Port, Is.EqualTo( CoordinatorOptions.DefaultPort ) );
      Assert.That( options.AgentPort, Is.EqualTo( CoordinatorOptions.DefaultAgentPort ) );
      Assert.That( options.ListenForAgents, Is.True );
      Assert.That( options.RequestLogging.Enabled, Is.True );
    }
  }

  [Test]
  public void Bind_OverridesDefaults_CaseInsensitively() {
    var options = CoordinatorOptions.Bind(
      Build( ( "PORT", "1234" ), ( "listenforagents", "false" ), ( "RequestLogging:Enabled", "false" ) )
    );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( options.Port, Is.EqualTo( 1234 ) );
      Assert.That( options.ListenForAgents, Is.False );
      Assert.That( options.RequestLogging.Enabled, Is.False );
    }
  }

  [Test]
  public void Bind_SchemaKey_IsIgnored() {
    Assert.DoesNotThrow( () => CoordinatorOptions.Bind( Build( ( "$schema", "https://example.com" ) ) ) );
  }

  [TestCase( "Prot", "1234" )]
  [TestCase( "RequestLogging:Enable", "false" )]
  public void Bind_UnknownKey_Throws( string key, string value ) {
    var exception =
      Assert.Throws<InvalidOperationException>( () => CoordinatorOptions.Bind( Build( ( key, value ) ) ) );
    Assert.That( exception.Message, Does.Contain( key[( key.LastIndexOf( ':' ) + 1 )..] ) );
  }

  [TestCase( (ushort) 0, (ushort) 2, "Port" )]
  [TestCase( (ushort) 1, (ushort) 0, "AgentPort" )]
  [TestCase( (ushort) 1, (ushort) 1, "must differ" )]
  public void Validate_Invalid_ReportsFailure( ushort port, ushort agentPort, string expected ) {
    var options = new CoordinatorOptions { Port = port, AgentPort = agentPort };

    Assert.That( options.Validate(), Has.Some.Contains( expected ) );
  }

  [Test]
  public void Validate_SamePorts_WithoutAgentListener_IsValid() {
    var options = new CoordinatorOptions { Port = 1, AgentPort = 1, ListenForAgents = false };

    Assert.That( options.Validate(), Is.Empty );
  }

  private static IConfiguration Build( params (string Key, string Value)[] values ) {
    return new ConfigurationBuilder()
      .AddInMemoryCollection( values.Select( v => new KeyValuePair<string, string?>( v.Key, v.Value ) ) )
      .Build();
  }
}