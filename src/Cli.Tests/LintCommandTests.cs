using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Globalization;
using Drift.Cli.Abstractions;

namespace Drift.Cli.Tests;

public class LintCommandTests {
  [Test, Combinatorial]
  public async Task LintValidSpec(
    [Values( "network_single_subnet" )] string specName,
    [Values( "", "normal", "log" )] string outputFormat
  ) {
    var originalOut = Console.Out;

    try {
      // Arrange
      var console = new TestConsole();
      Console.SetOut( console.Out.CreateTextWriter() );
      var parser = RootCommandFactory.CreateParser();
      var outputOption = string.IsNullOrWhiteSpace( outputFormat ) ? "" : $" -o {outputFormat}";

      // Act
      var result = await parser.InvokeAsync(
        $"lint ../../../../Spec.Tests/resources/{specName}.yaml" + outputOption,
        console
      );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( result, Is.EqualTo( ExitCodes.Success ) );
        await Verify( console.Out.ToString() + console.Error )
          .ScrubInlineDateTimes( "HH:mm:ss", CultureInfo.InvariantCulture );
      }
    }
    finally {
      Console.SetOut( originalOut );
    }
  }

  [Test, Combinatorial]
  public async Task LintInvalidSpec(
    [Values( "network_single_device_host" )]
    string specName,
    [Values( "", "normal", "log" )] string outputFormat
  ) {
    var originalOut = Console.Out;

    try {
      // Arrange
      var console = new TestConsole();
      Console.SetOut( console.Out.CreateTextWriter() );
      var parser = RootCommandFactory.CreateParser();
      var outputOption = string.IsNullOrWhiteSpace( outputFormat ) ? "" : $" -o {outputFormat}";

      // Act
      var result = await parser.InvokeAsync(
        $"lint ../../../../Spec.Tests/resources/{specName}.yaml" + outputOption,
        console
      );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( result, Is.EqualTo( ExitCodes.ValidationError ) );
        await Verify( console.Out.ToString() + console.Error )
          .ScrubInlineDateTimes( "HH:mm:ss", CultureInfo.InvariantCulture );
      }
    }
    finally {
      Console.SetOut( originalOut );
    }
  }

  [Test]
  public async Task LintMissingSpec() {
    // Arrange
    var parser = RootCommandFactory.CreateParser();

    // Act
    var result = await parser.InvokeAsync( "lint" );

    // Assert
    Assert.That( result, Is.EqualTo( ExitCodes.GeneralError ) );
  }
}