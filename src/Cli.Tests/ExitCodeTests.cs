using System.CommandLine;
using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands;
using Drift.Cli.Commands.Common;
using Drift.Cli.Output.Abstractions;

namespace Drift.Cli.Tests;

public class ExitCodeTests {
  private const string ExitCodeCommand = "exitcode";
  private const int ExitCodeCommandExitCode = 1337;

  private const string ExceptionThrowingCommand = "exceptionthrowing";

  private const string NonExistingCommand = "nonexisting";

  [Test]
  public async Task ExitCodeIsReturnedFromCommandHandlerTest() {
    // Arrange
    RootCommandFactory.CommandRegistration[] customCommands = [
      new(typeof(ExitCodeCommandHandler), sp => new ExitCodeTestCommand( sp ))
    ];

    // Act
    var (exitCode, output, error) =
      await DriftTestCli.InvokeFromTestAsync( ExitCodeCommand, customCommands: customCommands );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodeCommandExitCode ) );
      await Verify( output.ToString() + error );
    }
  }

  [Test]
  public async Task NonExistingCommand_ReturnsSystemCommandLineDefaultErrorTest() {
    // Arrange
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync( NonExistingCommand );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      await Verify( output.ToString() + error ).ScrubLinesWithReplace( line =>
        Regex.Replace(
          line,
          @"  .+ \[command\] \[options\]",
          "  <usage>"
        )
      );
      Assert.That( exitCode, Is.EqualTo( ExitCodes.SystemCommandLineDefaultError ) );
    }
  }

  [Test]
  public async Task UnhandledExceptionReturnsUnknownErrorTest() {
    // Arrange
    RootCommandFactory.CommandRegistration[] customCommands = [
      new(typeof(ExceptionCommandHandler), sp => new ExceptionTestCommand( sp ))
    ];

    // Act
    var (exitCode, output, error) =
      await DriftTestCli.InvokeFromTestAsync( ExceptionThrowingCommand, customCommands: customCommands );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.UnknownError ) );
      await Verify( output.ToString() + error );
    }
  }

  private class ExitCodeTestCommand( IServiceProvider provider )
    : CommandBase<DefaultParameters, ExitCodeCommandHandler>(
      ExitCodeCommand,
      "Command that returns a specific exit code",
      provider
    ) {
    protected override DefaultParameters CreateParameters( ParseResult result ) {
      return new DefaultParameters( result );
    }
  }

  private class ExitCodeCommandHandler( IOutputManager output ) : ICommandHandler<DefaultParameters> {
    public Task<int> Invoke( DefaultParameters parameters, CancellationToken cancellationToken ) {
      output.Normal.Write( $"Output from command '{ExitCodeCommand}'" );
      return Task.FromResult( ExitCodeCommandExitCode );
    }
  }

  private class ExceptionTestCommand( IServiceProvider provider )
    : CommandBase<DefaultParameters, ExceptionCommandHandler>(
      ExceptionThrowingCommand,
      "Command that throws an exception",
      provider
    ) {
    protected override DefaultParameters CreateParameters( ParseResult result ) {
      return new DefaultParameters( result );
    }
  }

  private class ExceptionCommandHandler : ICommandHandler<DefaultParameters> {
    public Task<int> Invoke( DefaultParameters parameters, CancellationToken cancellationToken ) {
      throw new Exception( $"This exception was thrown from {nameof(ExceptionCommandHandler)}" );
    }
  }
}