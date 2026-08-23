using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class EnvCommandTests {
  [Test]
  public async Task EnvWorkflow_AddMultipleAndManage_Works() {
    // Arrange / Act - add env1
    var (exitCode1, _, _) = await InvokeAsync( "env add env1 host1:5000" );
    Assert.That( exitCode1, Is.EqualTo( ExitCodes.Success ) );

    // Act - add env2
    var (exitCode2, _, _) = await InvokeAsync( "env add env2 host2:5000" );
    Assert.That( exitCode2, Is.EqualTo( ExitCodes.Success ) );

    // Act - switch to env2
    var (exitCode3, _, _) = await InvokeAsync( "env use env2" );
    Assert.That( exitCode3, Is.EqualTo( ExitCodes.Success ) );

    // Act - list
    var (exitCode4, output, error) = await InvokeAsync( "env list" );

    // Assert
    await Verify( output.ToString() + error );
    Assert.That( exitCode4, Is.EqualTo( ExitCodes.Success ) );

    var settings = ReadSettings();
    Assert.That( settings.Environments, Has.Count.EqualTo( 2 ) );
    Assert.That( settings.ActiveEnvironment, Is.EqualTo( "env2" ) );
  }
}
