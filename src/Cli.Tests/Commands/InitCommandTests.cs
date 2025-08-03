using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Scan.Subnet;
using Drift.Cli.Tests.Utils;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Spec.Schema;
using Drift.Spec.Validation;
using Drift.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Tests.Commands;

public class InitCommandTests {
  private static readonly ScanResult ScanResult = new ScanResult {
    Metadata =
      new Metadata {
        StartedAt = DateTime.Parse( "2025-06-11T12:20:08.4219405+02:00" ).ToUniversalTime(),
        EndedAt = DateTime.Parse( "2025-06-11" )
      },
    Status = ScanResultStatus.Success,
    DiscoveredDevices = [
      new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
      new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.11" )] },
      new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.12" )] }
    ]
  };

  [Test]
  public async Task MissingNameOption() {
    // Arrange
    var config = TestCommandLineConfiguration.Create();

    // Act
    var result = await config.InvokeAsync( "init" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result, Is.EqualTo( ExitCodes.GeneralError ) );
      await Verify( config.Output.ToString() + config.Error );
    }
  }

  [Combinatorial]
  [Test]
  public async Task GenerateSpecWithDiscoverySuccess(
    [Values( "", "-o log" )] string outputFormat,
    [Values( "", "-v" )] string verbose
  ) {
    // Arrange
    const string specName = "myNetworkWithDiscovery";
    DeleteSpec( specName );
    var config = TestCommandLineConfiguration.Create( services => {
        services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner( ScanResult ) );
      }
    );

    // Act
    var result = await config.InvokeAsync( $"init {specName} --discover {outputFormat} {verbose}" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result, Is.EqualTo( ExitCodes.Success ) );
      var verifyOutputTask = Verify( config.Output.ToString() + config.Error );
      if ( outputFormat == "-o log" ) {
        verifyOutputTask.ScrubInlineDateTimes( "HH:mm:ss" );
      }

      await verifyOutputTask;
      //await Verify( await File.ReadAllTextAsync( $"{specName}.spec.yaml" ) ).UseTextForParameters( "spec" );
    }
  }

// TODO merge with previous test? 
  [Test]
  public async Task GenerateSpecWithoutDiscoverySuccess() {
    // Arrange
    const string specName = "myNetworkWithoutDiscovery";
    DeleteSpec( specName );
    var config = TestCommandLineConfiguration.Create( services => {
        services.AddScoped<INetworkScanner>( _ => new PredefinedResultNetworkScanner( ScanResult ) );
      }
    );

    // Act
    var result = await config.InvokeAsync( $"init {specName}" );

    // Assert
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result, Is.EqualTo( ExitCodes.Success ) );
      await Verify( config.Output.ToString() + config.Error );
      await Verify( await File.ReadAllTextAsync( $"{specName}.spec.yaml" ) ).UseTextForParameters( "spec" );
    }
  }

  [Test]
  public async Task GeneratedSpecWithDiscoveryIsValid() {
    // Arrange
    var subnets = new List<CidrBlock> { new("192.168.0.0/24") };
    var subnetProvider = new DeclaredSubnetProvider( subnets.Select( CidrBlockExtensions.ToDeclared ) );

    var path = Path.GetTempFileName();

    // Act
    InitCommandHandler.CreateSpecWithDiscovery( ScanResult, subnetProvider.Get(), path );
    var yaml = File.ReadAllText( path );

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
    var yaml = File.ReadAllText( path );

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