using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan.Interactive.Input;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets.Interface;
using Drift.Scanning.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NetworkInterface = Drift.Scanning.Subnets.Interface.NetworkInterface;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ScanInteractiveTests {
  private static readonly INetworkInterface DefaultInterface = new NetworkInterface {
    Description = "eth0",
    OperationalStatus = System.Net.NetworkInformation.OperationalStatus.Up,
    UnicastAddress = new CidrBlock( "192.168.0.0/24" )
  };

  [Test]
  public async Task Interactive_QuitKey_ExitsSuccessfully() {
    // Arrange
    var services = ConfigureServices( configuredServices =>
      configuredServices.AddScoped<IConsoleKeyWatcher>( _ =>
        new PredefinedConsoleKeyWatcher( ConsoleKey.Q )
      )
    );

    // Act
    var (exitCode, _, error) = await DriftTestCli.InvokeAsync( "scan -i", services );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      Assert.That( error.ToString(), Is.Empty );
    }
  }

  private static Action<IServiceCollection> ConfigureServices(
    Action<IServiceCollection> configureInteractiveServices
  ) {
    return services => {
      services.AddScoped<IInterfaceSubnetProvider>( _ =>
        new PredefinedInterfaceSubnetProvider( [DefaultInterface] )
      );
      services.AddScoped<IScanOrchestrator>( _ => new PredefinedScanOrchestrator(
          new NetworkScanResult {
            Metadata = new Metadata { StartedAt = default, EndedAt = default },
            Status = ScanResultStatus.Success,
            Subnets = [
              new SubnetScanResult {
                CidrBlock = DefaultInterface.UnicastAddress!.Value,
                DiscoveredDevices = [],
                Metadata = new Metadata { StartedAt = default, EndedAt = default },
                Status = ScanResultStatus.Success
              }
            ]
          }
        )
      );
      services.AddScoped<IConsoleResizeWatcher, NoopConsoleResizeWatcher>();
      configureInteractiveServices( services );
    };
  }

  private sealed class PredefinedConsoleKeyWatcher( ConsoleKey key ) : IConsoleKeyWatcher {
    private bool _consumed;

    public Task WaitForKeyAsync() => Task.CompletedTask;

    public ConsoleKey? Consume() {
      if ( _consumed ) {
        return null;
      }

      _consumed = true;
      return key;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  private sealed class NoopConsoleResizeWatcher : IConsoleResizeWatcher {
    public Task WaitForResizeAsync() => Task.Delay( Timeout.InfiniteTimeSpan );

    public void Dispose() {
    }
  }
}