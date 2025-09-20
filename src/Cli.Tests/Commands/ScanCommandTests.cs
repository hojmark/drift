using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan.Rendering;
using Drift.Core.Scan.Subnets.Interface;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using NetworkInterface = Drift.Core.Scan.Subnets.Interface.NetworkInterface;

namespace Drift.Cli.Tests.Commands;

public class ScanCommandTests {
  private static readonly INetworkInterface DefaultInterface = new NetworkInterface {
    Description = "eth0", OperationalStatus = OperationalStatus.Up, UnicastAddress = new CidrBlock( "192.168.0.0/24" )
  };

  private static IEnumerable<TestCaseData> DiscoveredDeviceLists {
    get {
      yield return new TestCaseData( new List<DiscoveredDevice>(), new List<INetworkInterface> { DefaultInterface } )
        .SetName( "No devices" );

      yield return new TestCaseData(
          new List<DiscoveredDevice> { new() { Addresses = [new IpV4Address( "192.168.0.5" )] } },
          new List<INetworkInterface> { DefaultInterface }
        )
        .SetName( "Single device" );

      yield return new TestCaseData(
          new List<DiscoveredDevice> {
            new() { Addresses = [new IpV4Address( "192.168.0.10" )] },
            new() { Addresses = [new IpV4Address( "192.168.0.20" ), new MacAddress( "7d:fb:d0:e6:80:ae" )] },
            new() { Addresses = [new IpV4Address( "192.168.0.30" )] }
          },
          new List<INetworkInterface> { DefaultInterface }
        )
        .SetName( "Multiple devices" );

      yield return new TestCaseData(
          new List<DiscoveredDevice> {
            new() { Addresses = [new IpV4Address( "192.168.32.10" )] },
            new() { Addresses = [new IpV4Address( "192.168.32.20" ), new MacAddress( "7d:fb:d0:e6:80:ae" )] },
            new() { Addresses = [new IpV4Address( "192.168.34.4" )] },
            new() { Addresses = [new IpV4Address( "172.19.0.10" )] }
          },
          new List<INetworkInterface> {
            new NetworkInterface {
              Description = "eth1",
              OperationalStatus = OperationalStatus.Up,
              UnicastAddress = new CidrBlock( "10.255.255.254/32" )
            },
            new NetworkInterface {
              Description = "eth2",
              OperationalStatus = OperationalStatus.Up,
              UnicastAddress = new CidrBlock( "192.168.32.0/20" )
            },
            new NetworkInterface {
              Description = "eth3",
              OperationalStatus = OperationalStatus.Up,
              UnicastAddress = new CidrBlock( "172.19.0.0/16" )
            }
          }
        )
        .SetName( "Multiple devices, multiple subnets" );
    }
  }

  [OneTimeSetUp]
  public void SetupOnce() {
    // Easier to validate in snapshots
    NormalScanRenderer.IdMarkingStyle = IdMarkingStyle.Dot;
  }


  [Test]
  public async Task NonExistingSpecOption() {
    // Arrange / Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync( "scan blah_spec.yaml" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
      await Verify( output.ToString() + error );
    }
  }
/*
  private static Action<IServiceCollection> ConfigureServices(
    List<INetworkInterface> interfaces,
    List<DiscoveredDevice>? discoveredDevices = null,
    Inventory? inventory = null
  ) {
    return services => {
      services.AddScoped<IInterfaceSubnetProvider>( _ =>
        new PredefinedInterfaceSubnetProvider( interfaces )
      );

      if ( inventory != null ) {
        services.AddScoped<ISpecFileProvider>( _ =>
          new PredefinedSpecProvider( new Dictionary<string, Inventory> { { "unittest", inventory } } )
        );
      }

      services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner(
          new NetworkScanResult {
            Metadata = new Metadata { StartedAt = default, EndedAt = default },
            Status = ScanResultStatus.Success,
            DiscoveredDevices = discoveredDevices ?? []
          }
        )
      );
    };
  }*/
}