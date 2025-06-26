using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Lint;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Drift.Cli.Tests;

public class LintCommandTests {
  [TestCase( "network_single_subnet" )]
  public async Task LintValidSpec( string specName ) {
    // Arrange
    var outputConsole = new TestConsole();
    var parser = CreateParser( outputConsole );

    // Act
    var result = await parser.InvokeAsync(
      //TODO test -o log
      $"lint ../../../../Spec.Tests/resources/{specName}.yaml",
      outputConsole
    );

    // Assert
    Assert.That( result, Is.EqualTo( ExitCodes.Success ) );
    await Verify( outputConsole.Out.ToString() + outputConsole.Error );
  }

  [TestCase( "network_single_device_host" )]
  [Test]
  public async Task LintInvalidSpec( string specName ) {
    // Arrange
    var outputConsole = new TestConsole();
    var parser = CreateParser( outputConsole );

    // Act
    var result = await parser.InvokeAsync(
      //TODO test -o log
      $"lint ../../../../Spec.Tests/resources/{specName}.yaml",
      outputConsole
    );

    // Assert
    Assert.That( result, Is.EqualTo( ExitCodes.ValidationError ) );
    await Verify( outputConsole.Out.ToString() + outputConsole.Error );
  }

  [Test]
  public void LintMissingSpec() {
    // Arrange
    var parser = CreateParser( new TestConsole() );

    // Act / Assert
    Assert.ThrowsAsync<FileNotFoundException>( () => parser.InvokeAsync( "lint" ) );
  }

  private static Parser CreateParser( TestConsole console ) {
    var loggerConfig = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .Enrich.FromLogContext()
      .WriteTo.Console();
    //.WriteTo.TextWriter( console.Out.CreateTextWriter() );

    Log.Logger = loggerConfig
      .CreateLogger();

    var loggerFactory = LoggerFactory.Create( builder => builder.AddSerilog()
        .SetMinimumLevel( LogLevel.Debug ) // Parse from args?
      /*.AddSimpleConsole( config => {
        config.SingleLine = true;
        config.TimestampFormat = "[HH:mm:ss.ffff] ";
      } )*/
    );

    //TODO 'from' or 'against'?
    var rootCommand = new RootCommand( "📡\uFE0F Drift CLI — monitor network drift against your declared state" );
    rootCommand.AddCommand( new LintCommand( loggerFactory ) );

    var parser = new Parser( rootCommand );

    return parser;
  }
}