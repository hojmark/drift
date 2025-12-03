using System.Collections.Immutable;
using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Init.Helpers;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.SpecFile;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets.Interface;
using Drift.Scanning.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NetworkInterface = Drift.Scanning.Subnets.Interface.NetworkInterface;

namespace Drift.Cli.Tests.Commands;

internal sealed class ScanCommandTests {
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

  private static IEnumerable<TestCaseData> WithSpecList {
    get {
      yield return new TestCaseData( new NetworkBuilder().Build(), new List<DiscoveredDevice>() )
        .SetName( "Empty spec, no devices" );

      yield return new TestCaseData(
          new NetworkBuilder()
            .AddDevice( [new MacAddress( "10:10:10:10:10:10", isId: true )], "device1" )
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
                new MacAddress( "10:10:10:10:10:10", isId: true ),
                new IpV4Address( "192.168.0.10", isId: false )
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

      yield return new TestCaseData(
          new NetworkBuilder()
            .AddDevice(
              [
                new MacAddress( "10:10:10:10:10:10", isId: true ),
                new IpV4Address( "192.168.0.10", isId: false )
              ],
              "device1",
              state: DeclaredDeviceState.Dynamic
            )
            .AddDevice(
              [
                new MacAddress( "20:20:20:20:20:20", isId: true ),
              ],
              "device2",
              state: DeclaredDeviceState.Dynamic
            )
            .Build(),
          new List<DiscoveredDevice> { new() { Addresses = [new IpV4Address( "192.168.0.70" )] } }
        )
        .SetName( "Zero matches, two MACs declared" );

      yield return new TestCaseData(
          new NetworkBuilder()
            .AddDevice(
              [
                new MacAddress( "10:10:10:10:10:10", isId: true ),
                new IpV4Address( "192.168.0.10", isId: false )
              ],
              "device1",
              state: DeclaredDeviceState.Dynamic
            )
            .Build(),
          new List<DiscoveredDevice> {
            new() { Addresses = [new IpV4Address( "192.168.0.10" ), new MacAddress( "11:11:11:11:11:11" )] }
          }
        )
        .SetName( "Zero matches, discovered IP doesn't match declared IP" );
    }
  }

  [OneTimeSetUp]
  public void SetupOnce() {
    // Easier to validate in snapshots
    DeviceIdHighlighter.Style = IdMarkingStyle.Dot;
  }

  // [Combinatorial]
  [TestCaseSource( nameof(DiscoveredDeviceLists) )]
  public async Task WithoutSpec_Success_Test(
    List<DiscoveredDevice> discoveredDevices,
    List<INetworkInterface> interfaces
    /*, [Values( "", "normal", "log" )] string outputFormat */
  ) {
    // Arrange
    var serviceConfig = ConfigureServices( interfaces, discoveredDevices );

    // Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync( "scan", serviceConfig );
    // var exitCode = await config.InvokeAsync( $"scan -o {outputFormat}" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() + error )
        .UseFileName(
          $"{nameof(ScanCommandTests)}.{nameof(WithoutSpec_Success_Test)}.{TestContext.CurrentContext.Test.Name}"
        );
    }
  }

  [TestCaseSource( nameof(WithSpecList) )]
  public async Task WithSpec_Success_Test(
    Network network,
    List<DiscoveredDevice> discoveredDevices
  ) {
    // Arrange
    var serviceConfig = ConfigureServices(
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
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync( "scan unittest", serviceConfig );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() + error )
        .UseFileName(
          $"{nameof(ScanCommandTests)}.{nameof(WithSpec_Success_Test)}.{TestContext.CurrentContext.Test.Name}" );
    }
  }

  [Test]
  public async Task RemoteScan() {
    // Arrange
    var serviceConfigScan = ConfigureServices(
      [
        new NetworkInterface {
          Description = "eth1",
          OperationalStatus = OperationalStatus.Up,
          UnicastAddress = new CidrBlock( "192.168.0.0/24" )
        }
      ],
      [
        new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.100" ), new MacAddress( "11:11:11:11:11:11" )] }
      ],
      new Inventory {
        Network = new Network(), Agents = [new Domain.Agent { Id = "local1", Address = "http://localhost:51515" }]
      }
    );

    var serviceConfigAgent = ConfigureServices(
      [
        new NetworkInterface {
          Description = "eth1",
          OperationalStatus = OperationalStatus.Up,
          UnicastAddress = new CidrBlock( "192.168.100.0/24" )
        }
      ],
      [
        new DiscoveredDevice {
          Addresses = [new IpV4Address( "192.168.100.200" ), new MacAddress( "22:22:22:22:22:22" )]
        }
      ]
    );

    var cts = new CancellationTokenSource( TimeSpan.FromSeconds( 800 ) );

    // Act
    Console.WriteLine( "Invoking agent start" );
    var agentTask = DriftTestCli.InvokeFromTestAsync(
      "agent start --adoptable -v",
      serviceConfigAgent,
      cancellationToken: cts.Token
    );
    await Task.Delay( 3000, cts.Token );
    Console.WriteLine( "Invoking scan" );
    var (scanExitCode, scanOutput, scanError) = await DriftTestCli.InvokeFromTestAsync(
      "scan unittest",
      serviceConfigScan,
      cancellationToken: cts.Token
    );
    Console.WriteLine( "Scan finished" );
    Console.WriteLine( "----------------" );
    Console.WriteLine( scanOutput.ToString() + scanError );
    Console.WriteLine( "----------------" );

    Console.WriteLine( "Cancelling token" );
    await cts.CancelAsync();
    cts.Dispose();
    Console.WriteLine( "Waiting for agent to shut down" );

    var (agentExitCode, agentOutput, agentError) = await agentTask;

    Console.WriteLine( "Agent finished" );
    Console.WriteLine( "----------------" );
    Console.WriteLine( agentOutput.ToString() + agentError );
    Console.WriteLine( "----------------" );

    // Assert
    Assert.That( agentExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( scanExitCode, Is.EqualTo( ExitCodes.Success ) );
    await Verify( scanOutput.ToString() + scanError );
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

  private static Action<IServiceCollection> ConfigureServices(
    List<INetworkInterface> interfaces,
    List<DiscoveredDevice> discoveredDevices,
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
            Subnets = [
              new SubnetScanResult {
                CidrBlock = DefaultInterface.UnicastAddress!.Value,
                DiscoveredDevices = discoveredDevices,
                Metadata = new Metadata { StartedAt = default, EndedAt = default },
                Status = ScanResultStatus.Success,
                // TODO could/should also include ip's of non-discovered devices?
                DiscoveryAttempts = discoveredDevices.Select( d =>
                    new IpV4Address( d.Get( AddressType.IpV4 ) ?? throw new Exception( "Device had no IPv4" ) )
                  )
                  .ToImmutableHashSet()
              }
            ]
          }
        )
      );
    };
  }
}