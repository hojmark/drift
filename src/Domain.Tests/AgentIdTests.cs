namespace Drift.Domain.Tests;

internal sealed class AgentIdTests {
  [TestCase( "agent_one" )]
  [TestCase( "agent_subnet_agent1" )]
  [TestCase( "AGENT_One" )]
  [TestCase( "agent_host-1.lan" )]
  [TestCase( "agent_3f2504e0-4f89-11d3-9a0c-0305e82c3301" )]
  public void Parse_ValidId_Succeeds( string value ) {
    Assert.That( AgentId.Parse( value, null ).Value, Is.EqualTo( value ) );
  }

  [Test]
  public void New_IsValidAndGuidBased() {
    var id = AgentId.New();

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( AgentId.TryParse( id.Value, null, out _ ), Is.True );
      Assert.That( id.IsGuidBased, Is.True );
    }
  }

  [TestCase( "", TestName = "Empty" )]
  [TestCase( "agent_", TestName = "Prefix only" )]
  [TestCase( "one", TestName = "Missing prefix" )]
  [TestCase( "agent_one\n[INF] forged log line", TestName = "Line feed" )]
  [TestCase( "agent_one\r", TestName = "Carriage return" )]
  [TestCase( "agent_one\u001b[31m", TestName = "ANSI escape" )]
  [TestCase( "agent_one two", TestName = "Space" )]
  [TestCase( "agent_../../etc", TestName = "Path separator" )]
  [TestCase( "agent_ø", TestName = "Non-ASCII letter" )]
  public void Parse_InvalidId_ThrowsFormatException( string value ) {
    Assert.Throws<FormatException>( () => AgentId.Parse( value, null ) );
  }

  [Test]
  public void Parse_TooLongId_ThrowsFormatException() {
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( () => AgentId.Parse( "agent_" + new string( 'a', 64 ), null ), Throws.Nothing );
      Assert.That( () => AgentId.Parse( "agent_" + new string( 'a', 65 ), null ), Throws.TypeOf<FormatException>() );
    }
  }

  [Test]
  public void Parse_InvalidId_DoesNotEchoValueInMessage() {
    var exception = Assert.Throws<FormatException>( () => AgentId.Parse( "agent_x\nforged", null ) );

    Assert.That( exception.Message, Does.Not.Contain( "forged" ) );
  }

  [Test]
  public void TryParse_InvalidId_ReturnsFalse() {
    Assert.That( AgentId.TryParse( "agent_one\nforged", null, out _ ), Is.False );
  }
}