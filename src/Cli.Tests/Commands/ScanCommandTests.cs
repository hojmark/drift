using Drift.Cli.Abstractions;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests.Commands;

public class ScanCommandTests {
  private static IEnumerable<TestCaseData> DiscoveredDeviceLists {
    get {
      yield return new TestCaseData( new List<DiscoveredDevice>() )
        .SetName( "No devices" );

      yield return new TestCaseData( new List<DiscoveredDevice> {
          new() { Addresses = [new IpV4Address( "192.168.0.5" )] }
        } )
        .SetName( "Single device" );

      yield return new TestCaseData( new List<DiscoveredDevice> {
          new() { Addresses = [new IpV4Address( "192.168.0.10" )] },
          new() { Addresses = [new IpV4Address( "192.168.0.20" ), new MacAddress( "7d:fb:d0:e6:80:ae" )] },
          new() { Addresses = [new IpV4Address( "192.168.0.30" )] }
        } )
        .SetName( "Multiple devices" );
    }
  }

  //[Combinatorial]
  [TestCaseSource( nameof(DiscoveredDeviceLists) )]
  public async Task SuccessTest(
    List<DiscoveredDevice> devices /*, [Values( "", "normal", "log" )] string outputFormat */
  ) {
    // Arrange
    var config = TestCommandLineConfiguration.Create( services => {
        services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner(
            new ScanResult {
              Metadata = new Metadata { StartedAt = default, EndedAt = default },
              Status = ScanResultStatus.Success,
              DiscoveredDevices = devices
            }
          )
        );
      }
    );

    // Act
    var exitCode = await config.InvokeAsync( "scan" );
    //var exitCode = await config.InvokeAsync( $"scan -o {outputFormat}" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( config.Output.ToString() + config.Error )
        .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(SuccessTest)}.{TestContext.CurrentContext.Test.Name}" );
    }
  }

  [Test]
  public async Task NonExistingSpecOption() {
    // Arrange
    var config = TestCommandLineConfiguration.Create();

    // Act
    var exitCode = await config.InvokeAsync( "scan blah_spec.yaml" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
      await Verify( config.Output.ToString() + config.Error );
    }
  }
}