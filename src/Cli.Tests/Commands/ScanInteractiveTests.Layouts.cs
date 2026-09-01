using Drift.Cli.Commands.Scan.Interactive.Ui;
using Drift.Cli.Commands.Scan.Models;
using Drift.Cli.Commands.Scan.ResultProcessors;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Scan;
using Spectre.Console;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ScanInteractiveTests {
  [Test]
  public async Task Layout_RendersExpectedScreen() {
    var output = new StringWriter();
    var console = AnsiConsole.Create( new AnsiConsoleSettings {
      Out = new AnsiConsoleOutput( output ),
      Ansi = AnsiSupport.No,
      ColorSystem = ColorSystemSupport.NoColors,
      Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
    } );
    console.Profile.Width = 90;
    console.Profile.Height = 20;

    var layout = new ScanLayout( new NetworkId( "test-network" ), console );
    var cidr = new CidrBlock( "192.168.0.0/24" );
    layout.SetScanTree(
      TreeRenderer.Render(
        [new Subnet { Cidr = cidr, Devices = [], IsExpanded = false }],
        cidr,
        layout.AvailableRows,
        0
      )
    );
    layout.SetProgress( Percentage.Hundred );

    console.Write( layout.Renderable );

    await Verify( output.ToString().TrimStart( '\uFEFF' ).TrimEnd() );
  }

  [Test]
  public async Task Layout_RendersDiscoveredDevices() {
    var output = new StringWriter();
    var console = AnsiConsole.Create( new AnsiConsoleSettings {
      Out = new AnsiConsoleOutput( output ),
      Ansi = AnsiSupport.No,
      ColorSystem = ColorSystemSupport.NoColors,
      Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
    } );
    console.Profile.Width = 90;
    console.Profile.Height = 20;

    var cidr = new CidrBlock( "192.168.0.0/24" );
    var scanResult = new NetworkScanResult {
      Metadata = new Metadata { StartedAt = default, EndedAt = default },
      Status = ScanResultStatus.Success,
      Progress = Percentage.Hundred,
      Subnets = [
        new SubnetScanResult {
          CidrBlock = cidr,
          DiscoveredDevices = [
            new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.10" )] },
            new DiscoveredDevice { Addresses = [new IpV4Address( "192.168.0.20" )] }
          ],
          Metadata = new Metadata { StartedAt = default, EndedAt = default },
          Status = ScanResultStatus.Success
        }
      ]
    };
    var layout = new ScanLayout( new NetworkId( "test-network" ), console );
    layout.SetScanTree(
      TreeRenderer.Render(
        NetworkScanResultProcessor.Process( scanResult, null ),
        cidr,
        layout.AvailableRows,
        0
      )
    );
    layout.SetProgress( scanResult.Progress );

    console.Write( layout.Renderable );

    await Verify( output.ToString().TrimStart( '\uFEFF' ).TrimEnd() );
  }

  [Test]
  public async Task Layout_RendersEmptyStateWithoutCidrs() {
    var output = new StringWriter();
    var console = AnsiConsole.Create( new AnsiConsoleSettings {
      Out = new AnsiConsoleOutput( output ),
      Ansi = AnsiSupport.No,
      ColorSystem = ColorSystemSupport.NoColors,
      Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
    } );
    console.Profile.Width = 90;
    console.Profile.Height = 20;

    var layout = new ScanLayout( new NetworkId( "test-network" ), console );
    layout.SetScanTree( [] );
    layout.SetProgress( Percentage.Zero );

    console.Write( layout.Renderable );

    await Verify( output.ToString().TrimStart( '\uFEFF' ).TrimEnd() );
  }

  [Test]
  public async Task Layout_RendersCompletedEmptyStateWithoutCidrs() {
    var output = new StringWriter();
    var console = AnsiConsole.Create( new AnsiConsoleSettings {
      Out = new AnsiConsoleOutput( output ),
      Ansi = AnsiSupport.No,
      ColorSystem = ColorSystemSupport.NoColors,
      Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
    } );
    console.Profile.Width = 90;
    console.Profile.Height = 20;

    var layout = new ScanLayout( new NetworkId( "test-network" ), console );
    layout.SetScanTree( [] );
    layout.SetProgress( Percentage.Hundred );

    console.Write( layout.Renderable );

    await Verify( output.ToString().TrimStart( '\uFEFF' ).TrimEnd() );
  }
}