using System.CommandLine;
using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Scan.Subnet;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Microsoft.Extensions.DependencyInjection;
using NetworkInterface = Drift.Cli.Commands.Scan.Subnet.NetworkInterface;

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

  //[Combinatorial]
  [TestCaseSource( nameof(DiscoveredDeviceLists) )]
  public async Task WithoutSpec_Success_Test(
    List<DiscoveredDevice> discoveredDevices,
    List<INetworkInterface> interfaces
    /*, [Values( "", "normal", "log" )] string outputFormat */
  ) {
    // Arrange
    var config = GetCommandLineConfiguration( interfaces, discoveredDevices );

    // Act
    var exitCode = await config.InvokeAsync( "scan" );
    //var exitCode = await config.InvokeAsync( $"scan -o {outputFormat}" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( config.Output.ToString() + config.Error )
        .UseFileName(
          $"{nameof(ScanCommandTests)}.{nameof(WithoutSpec_Success_Test)}.{TestContext.CurrentContext.Test.Name}"
        );
    }
  }

  private static IEnumerable<TestCaseData> WithSpecList {
    get {
      yield return new TestCaseData( new NetworkBuilder().Build(), new List<DiscoveredDevice>() )
        .SetName( "Empty spec, no devices" );

      yield return new TestCaseData(
          new NetworkBuilder()
            .AddDevice( [new MacAddress( "10:10:10:10:10:10" )], "device1" )
            .AddDevice( [new MacAddress( "20:20:20:20:20:20" ), new IpV4Address( "192.168.0.20" )], "device2" )
            .Build(),
          new List<DiscoveredDevice> {
            new() { Addresses = [new MacAddress( "10:10:10:10:10:10" ), new IpV4Address( "192.168.0.10" )] }
          }
        )
        .SetName( "One MAC match, declared without IP" );

      yield return new TestCaseData(
          new NetworkBuilder()
            .AddDevice(
              [
                new MacAddress( "10:10:10:10:10:10" ) { IsId = true },
                new IpV4Address( "192.168.0.10" ) { IsId = false }
              ],
              "device1"
            )
            .AddDevice(
              [
                new MacAddress( "20:20:20:20:20:20" ),
                new IpV4Address( "192.168.0.20" )
              ],
              "device2"
            )
            .Build(),
          new List<DiscoveredDevice> {
            new() { Addresses = [new MacAddress( "10:10:10:10:10:10" ), new IpV4Address( "192.168.0.15" )] }
          }
        )
        .SetName( "One MAC match, discovered with different IP" );
    }
  }

  [TestCaseSource( nameof(WithSpecList) )]
  public async Task WithSpec_Success_Test(
    Network network,
    List<DiscoveredDevice> discoveredDevices
  ) {
    // Arrange
    var config = GetCommandLineConfiguration(
      [
        new NetworkInterface {
          Description = "eth1",
          OperationalStatus = OperationalStatus.Up,
          UnicastAddress = new CidrBlock( "192.168.0.0/24" )
        }
      ],
      discoveredDevices,
      new Inventory { Network = network }
    );

    // Act
    var exitCode = await config.InvokeAsync( "scan unittest" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( config.Output.ToString() + config.Error )
        .UseFileName(
          $"{nameof(ScanCommandTests)}.{nameof(WithSpec_Success_Test)}.{TestContext.CurrentContext.Test.Name}" );
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

  private static CommandLineConfiguration GetCommandLineConfiguration(
    List<INetworkInterface> interfaces,
    List<DiscoveredDevice>? discoveredDevices = null,
    Inventory? inventory = null
  ) {
    return TestCommandLineConfiguration.Create( services => {
        services.AddScoped<IInterfaceSubnetProvider>( sp =>
          new PredefinedInterfaceSubnetProvider( sp.GetRequiredService<IOutputManager>(), interfaces )
        );

        if ( inventory != null ) {
          services.AddScoped<ISpecFileProvider>( _ =>
            new PredefinedSpecProvider( new Dictionary<string, Inventory> { { "unittest", inventory } } )
          );
        }

        services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner(
            new ScanResult {
              Metadata = new Metadata { StartedAt = default, EndedAt = default },
              Status = ScanResultStatus.Success,
              DiscoveredDevices = discoveredDevices ?? []
            }
          )
        );
      }
    );
  }
}