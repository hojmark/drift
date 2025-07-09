using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Scan.Subnet;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Drift.Spec.Schema;
using Drift.Spec.Validation;
using Drift.TestUtilities;

namespace Drift.Cli.Tests;

public class InitCommandTests {
  [Test]
  public async Task MissingNameOption() {
    var originalOut = Console.Out;
    try {
      // Arrange
      var console = new TestConsole();
      Console.SetOut( console.Out );
      Console.SetError( console.Error );
      var parser = RootCommandFactory.CreateParser();

      // Act
      var result = await parser.InvokeAsync( "init" );

      // Assert
      using ( Assert.EnterMultipleScope() ) {
        Assert.That( result, Is.EqualTo( ExitCodes.GeneralError ) );
        await Verify( console.Out.ToString() + console.Error );
      }
    }
    finally {
      Console.SetOut( originalOut );
    }
  }

  [Test]
  public async Task GeneratedSpecWithDiscoveryIsValid() {
    // Arrange
    var scanResult = new ScanResult {
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
    var subnets = new List<CidrBlock> { new("192.168.0.0/24") };
    var subnetProvider = new DeclaredSubnetProvider( subnets.Select( CidrBlockExtensions.ToDeclared ) );

    var path = Path.GetTempFileName();

    // Act
    InitCommand.CreateSpecWithDiscovery( scanResult, subnetProvider, path );
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
    InitCommand.CreateSpecWithoutDiscovery( path );
    var yaml = File.ReadAllText( path );

    //Assert
    var validationResult = SpecValidator.Validate( yaml, SpecVersion.V1_preview );

    using ( Assert.EnterMultipleScope() ) {
      Assert.That( validationResult.IsValid, validationResult.ToUnitTestMessage() );
      await Verify( yaml );
    }
  }
}