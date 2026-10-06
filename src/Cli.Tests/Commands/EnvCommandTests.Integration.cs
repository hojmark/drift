using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  [Test]
  public async Task EnvWorkflow() {
    // Arrange / Act
    var (exitCode1, _, _) = await InvokeAsync( "env add env1 host1:5000" );
    var (exitCode2, _, _) = await InvokeAsync( "env add env2 host2:5000" );
    var (exitCode3, _, _) = await InvokeAsync( "env use env2" );
    var (exitCode4, output4, error4) = await InvokeAsync( "env list" );

    // Assert - exit codes
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode1, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( exitCode2, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( exitCode3, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( exitCode4, Is.EqualTo( ExitCodes.Success ) );
    }

    // Assert - output
    await Verify( output4.ToString() + error4 );

    // Assert - settings
    var settings = ReadSettings();
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( settings.Environments, Has.Count.EqualTo( 2 ) );
      Assert.That( settings.ActiveEnvironment, Is.EqualTo( "env2" ) );
    }
  }
}