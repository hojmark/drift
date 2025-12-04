using System.CommandLine;
using Drift.Cli.Commands.Common.Commands;
using Drift.Cli.Commands.Common.Parameters;
using Drift.Cli.Infrastructure;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Settings.Serialization;
using Drift.Cli.Settings.Tests;
using Drift.Cli.Settings.V1_preview;
using Drift.Cli.Settings.V1_preview.FeatureFlags;
using Drift.Cli.Tests.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Tests;

internal sealed class FeatureFlagTest {
  private const string DummyCodeCommand = "dummy";
  private const int DummyCommandExitCode = 1337;
  private static readonly FeatureFlag MyFeature = new("myFeature");
  private static readonly ISettingsLocationProvider SettingsLocationProvider = new TemporarySettingsLocationProvider();

  [Test]
  public async Task SettingsControlFlag( [Values( false, true, null )] bool? featureEnabled ) {
    // Arrange
    if ( Directory.Exists( SettingsLocationProvider.GetDirectory() ) ) {
      Directory.Delete( SettingsLocationProvider.GetDirectory(), true );
    }

    var settings = new CliSettings();
    if ( featureEnabled != null ) {
      settings.Features = [new FeatureFlagSetting( MyFeature, featureEnabled.Value )];
    }

    settings.Save( logger: NullLogger.Instance, location: SettingsLocationProvider );

    RootCommandFactory.CommandRegistration[] customCommands = [
      new(typeof(DummyTestCommandHandler), sp => new DummyTestCommand( sp ))
    ];

    // Act
    var result = await DriftTestCli.InvokeFromTestAsync( $"{DummyCodeCommand}", customCommands: customCommands );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.ExitCode, Is.EqualTo( DummyCommandExitCode ) );
      var expectedOutput = featureEnabled == true ? "Feature is enabled" : "Feature is disabled";
      Assert.That( result.Output.ToString(), Is.EqualTo( expectedOutput ) );
      Assert.That( result.Error.ToString(), Is.Empty );
    }

    Console.WriteLine( result.Output.ToString() );
  }

  private sealed class DummyTestCommand( IServiceProvider provider )
    : CommandBase<DummyTestParameters, DummyTestCommandHandler>(
      DummyCodeCommand,
      "Command that switches behavior using a feature flag",
      provider
    ) {
    protected override DummyTestParameters CreateParameters( ParseResult result ) {
      return new DummyTestParameters( result );
    }
  }

  private sealed class DummyTestCommandHandler( IOutputManager output ) : ICommandHandler<DummyTestParameters> {
    public Task<int> Invoke( DummyTestParameters parameters, CancellationToken cancellationToken ) {
      output.Normal.Write(
        CliSettings.Load( location: SettingsLocationProvider ).IsFeatureEnabled( MyFeature )
          ? "Feature is enabled"
          : "Feature is disabled"
      );

      return Task.FromResult( DummyCommandExitCode );
    }
  }

  private sealed record DummyTestParameters : BaseParameters {
    public DummyTestParameters( ParseResult parseResult ) : base( parseResult ) {
    }
  }
}