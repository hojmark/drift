using Drift.Cli.Abstractions;
using Drift.TestUtilities;

namespace Drift.Cli.Tests.Commands;

public class LintCommandTests {
  [Test, Combinatorial]
  public async Task LintValidSpec(
    [Values( "network_single_subnet" )] string specName,
    [Values( "", "normal", "log" )] string outputFormat
  ) {
    // Arrange
    var config = TestCommandLineConfiguration.Create();
    var outputOption = string.IsNullOrWhiteSpace( outputFormat ) ? "" : $" -o {outputFormat}";

    // Act
    var exitCode = await config.InvokeAsync(
      $"lint ../../../../Spec.Tests/resources/{specName}.yaml" + outputOption
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( config.Output.ToString() + config.Error )
        .ScrubLogOutputTime();
    }
  }

  [Test, Combinatorial]
  public async Task LintInvalidSpec(
    [Values( "network_single_device_host" )]
    string specName,
    [Values( "", "normal", "log" )] string outputFormat
  ) {
    // Arrange
    var config = TestCommandLineConfiguration.Create();
    var outputOption = string.IsNullOrWhiteSpace( outputFormat ) ? "" : $" -o {outputFormat}";

    // Act
    var exitCode = await config.InvokeAsync(
      $"lint ../../../../Spec.Tests/resources/{specName}.yaml" + outputOption
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.SpecValidationError ) );
      await Verify( config.Output.ToString() + config.Error )
        .ScrubLogOutputTime();
    }
  }

  [Test]
  public async Task LintMissingSpec() {
    // Arrange
    var config = TestCommandLineConfiguration.Create();

    // Act
    var exitCode = await config.InvokeAsync( "lint" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
  }
}