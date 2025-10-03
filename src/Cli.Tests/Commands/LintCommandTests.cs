using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.TestUtilities;

namespace Drift.Cli.Tests.Commands;

internal sealed class LintCommandTests {
  [Test]
  public async Task LintValidSpec(
    [Values( "network_single_subnet" )] string specName,
    [Values( "", "normal", "log" )] string outputFormat
  ) {
    // Arrange
    var outputOption = string.IsNullOrWhiteSpace( outputFormat ) ? string.Empty : $" -o {outputFormat}";

    // Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync(
      $"lint ../../../../Spec.Tests/resources/{specName}.yaml" + outputOption
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() + error )
        .ScrubLogOutputTime();
    }
  }

  [Test]
  public async Task LintInvalidSpec(
    [Values( "network_single_device_host" )]
    string specName,
    [Values( "", "normal", "log" )] string outputFormat
  ) {
    // Arrange
    var outputOption = string.IsNullOrWhiteSpace( outputFormat ) ? string.Empty : $" -o {outputFormat}";

    // Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync(
      $"lint ../../../../Spec.Tests/resources/{specName}.yaml" + outputOption
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.SpecValidationError ) );
      await Verify( output.ToString() + error )
        .ScrubLogOutputTime();
    }
  }

  [Test]
  public async Task LintMissingSpec() {
    // Arrange / Act
    var (exitCode, _, _) = await DriftTestCli.InvokeFromTestAsync( "lint" );

    // Assert
    Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
  }
}