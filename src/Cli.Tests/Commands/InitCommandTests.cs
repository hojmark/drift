using System.Net.NetworkInformation;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Init;
using Drift.Cli.Presentation.Output.Abstractions;
using Drift.Cli.Presentation.Output.Logging;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Scanning.Subnets;
using Drift.Scanning.Subnets.Interface;
using Drift.Scanning.Tests.Utils;
using Drift.Spec.Schema;
using Drift.Spec.Validation;
using Drift.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using NetworkInterface = Drift.Scanning.Subnets.Interface.NetworkInterface;

namespace Drift.Cli.Tests.Commands;

internal sealed class InitCommandTests {
  const string SpecNameWithDiscovery = "myNetworkWithDiscovery";
  const string SpecNameWithoutDiscovery = "myNetworkWithoutDiscovery";

  private static readonly NetworkScanResult ScanResult = new() {
    Metadata =
      new Domain.Scan.Metadata {
        StartedAt = DateTime.Parse( "2025-06-11T12:20:08.4219405+02:00" ).ToUniversalTime(),
        EndedAt = DateTime.Parse( "2025-06-11" )
      },
    Status = ScanResultStatus.Success,
    Subnets = [
      new SubnetScanResult {
        CidrBlock = new CidrBlock( "192.168.0.0/24" ),
        Metadata = null,
        Status = ScanResultStatus.Success,
        DiscoveredDevices = [
          new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
          new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.11" )] },
          new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.12" )] }
        ]
      }
    ]
  };

  private static readonly List<INetworkInterface> Interfaces = [
    new NetworkInterface {
      Description = "lo", OperationalStatus = OperationalStatus.Up, UnicastAddress = new CidrBlock( "127.0.0.0/8" )
    },
    new NetworkInterface {
      Description = "enp0xxxxx", OperationalStatus = OperationalStatus.Down, UnicastAddress = null
    },
    new NetworkInterface {
      Description = "enp8xxxxx",
      OperationalStatus = OperationalStatus.Up,
      UnicastAddress = new CidrBlock( "192.168.0.0/24" )
    },
    new NetworkInterface {
      Description = "wlp", OperationalStatus = OperationalStatus.Up, UnicastAddress = new CidrBlock( "192.168.0.0/24" )
    },
    new NetworkInterface {
      Description = "tun0", OperationalStatus = OperationalStatus.Up, UnicastAddress = new CidrBlock( "100.100.1.9/32" )
    }
  ];

  [TearDown]
  public void TearDown() {
    DeleteSpec( SpecNameWithDiscovery );
    DeleteSpec( SpecNameWithoutDiscovery );
  }

  [Test]
  public async Task MissingNameOption() {
    // Arrange / Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync( "init" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.GeneralError ) );
      await Verify( output.ToString() + error );
    }
  }

  [Combinatorial]
  [Test]
  public async Task GenerateSpecWithDiscoverySuccess(
    [Values( "", "-o log" )] string outputFormat,
    [Values( "", "-v" )] string verbose
  ) {
    // Arrange
    var serviceConfig = ( IServiceCollection services ) => {
      services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner( ScanResult ) );
      services.AddScoped<IInterfaceSubnetProvider>( sp =>
        new PredefinedInterfaceSubnetProvider( Interfaces, sp.GetRequiredService<IOutputManager>().GetLogger() )
      );
    };

    // Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync(
      $"init {SpecNameWithDiscovery} --discover {outputFormat} {verbose}",
      serviceConfig
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      var verifyOutputTask = Verify( output.ToString() + error );
      if ( outputFormat == "-o log" ) {
        await verifyOutputTask.ScrubLogOutputTime();
      }
      else {
        await verifyOutputTask;
      }

      //await Verify( await File.ReadAllTextAsync( $"{specName}.spec.yaml" ) ).UseTextForParameters( "spec" );
    }
  }

// TODO merge with previous test? 
  [Test]
  public async Task GenerateSpecWithoutDiscoverySuccess() {
    // Arrange
    var serviceConfig = ( IServiceCollection services ) => {
      services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner( ScanResult ) );
    };

    // Act
    var (exitCode, output, error) = await DriftTestCli.InvokeFromTestAsync(
      $"init {SpecNameWithoutDiscovery}",
      serviceConfig
    );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( exitCode, Is.EqualTo( ExitCodes.Success ) );
      await Verify( output.ToString() + error );
      await Verify( await File.ReadAllTextAsync( $"{SpecNameWithoutDiscovery}.spec.yaml" ) )
        .UseTextForParameters( "spec" );
    }
  }

  [Test]
  public async Task GeneratedSpecWithDiscoveryIsValid() {
    // Arrange
    var subnets = new List<CidrBlock> { new("192.168.0.0/24") };
    var subnetProvider = new PredefinedSubnetProvider( subnets.Select( CidrBlockExtensions.ToDeclared ) );

    var path = Path.GetTempFileName();

    // Act
    InitCommandHandler.CreateSpecWithDiscovery( ScanResult, subnetProvider.Get(), path );
    var yaml = await File.ReadAllTextAsync( path );

    //Assert
    var validationResult = SpecValidator.Validate( yaml, SpecVersion.V1_preview );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( validationResult.IsValid, validationResult.ToUnitTestMessage() );
      await Verify( yaml );
    }
  }

  [Test]
  public async Task GeneratedSpecWithoutDiscoveryIsValid() {
    // Arrange
    var path = Path.GetTempFileName();

    // Act
    InitCommandHandler.CreateSpecWithoutDiscovery( path );
    var yaml = await File.ReadAllTextAsync( path );

    //Assert
    var validationResult = SpecValidator.Validate( yaml, SpecVersion.V1_preview );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( validationResult.IsValid, validationResult.ToUnitTestMessage() );
      await Verify( yaml );
    }
  }

  private static void DeleteSpec( string specName ) {
    string fileName = $"{specName}.spec.yaml";

    if ( File.Exists( fileName ) ) {
      Console.WriteLine( $"Deleting existing file {fileName}" );
      File.Delete( fileName );
    }
  }
}