using Drift.Cli.Output.Abstractions;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Rendering;

//TODO use NormalOutput instead of Spectre directly
internal class NormalRenderer( INormalOutput console ) : DiffRendererBase {
  // Anonymising MACs e.g. for GitHub screenshot
  //TODO replace with more generic data simulation
  private const bool FakeMac = false;

  protected override void Render(
    List<ObjectDiff> differences,
    IEnumerable<DeclaredDevice> declaredDevices,
    ILogger? logger = null
  ) {
    if ( !differences.Any() ) {
      console.GetAnsiConsole().WriteLine( "No devices found" );
      return;
    }

    var showCustomIdColumn = declaredDevices.Any( d => d.Id != null );
    var table = CreateTable( showCustomIdColumn );

    AddDevices( table, differences, declaredDevices, showCustomIdColumn, logger );

    console.GetAnsiConsole().Write( table );

    //console.WriteLine( $"Total declared devices: {data.DevicesDeclared.Count()}" ); //TODO host vs device terminology
    //console.WriteLine( $"Total discovered devices: {data.DevicesDiscovered.Count()}" );
  }

  private static void AddDevices( Table table,
    List<ObjectDiff> differences,
    IEnumerable<DeclaredDevice> declaredDevices,
    bool? showCustomIdColumn = false,
    ILogger? logger = null
  ) {
    var directDeviceDifferences = GetDirectDeviceDifferences( differences );

    foreach ( var diff in directDeviceDifferences ) {
      logger?.LogTrace( "Device diff: {Action} {Path}", diff.DiffType, diff.PropertyPath );
      // Console.WriteLine( $"{diff.DiffType + ":",-10} {diff.PropertyPath}" );

      var state = diff.DiffType;

      var device = state switch {
        DiffType.Unchanged => ( (DiffDevice) diff.Original! ),
        DiffType.Removed => ( (DiffDevice) diff.Original! ),
        DiffType.Added => ( (DiffDevice) diff.Updated! ),
        _ => throw new Exception( "øv" )
      };

      /*
       * TODO target status:
       *
       * Icon:
       * - Closed circle: online
       * - Open circle: offline
       * - Question mark: unknown device (not in spec)
       * - Exclamation mark: unknown device (not in spec) that has been disallowed by general setting (unknown devices not allowed)
       *
       * Color:
       * - Green: expected state
       * - Red: opposite of expected state
       * - Yellow: undefined state (Q: both because the device is unknown AND because the state of a known device hasn't been specified)
       * Note: could have option for treating unknown devices as disallowed, thus red instead of default yellow
       *
       */

      var declaredDevice = declaredDevices
        .SingleOrDefault( d => d.Get( AddressType.IpV4 ) == device.Get( AddressType.IpV4 ) );
      var declaredDeviceState = declaredDevice?.State;
      var discoveredDeviceState =
        state == DiffType.Removed ? DiscoveredDeviceState.Offline : DiscoveredDeviceState.Online;

      const bool allowUnknownDevices = true;
      var status =
        GetStatusIcon( declaredDeviceState, discoveredDeviceState, state == DiffType.Added, allowUnknownDevices );
      var textStatus =
        GetStatusText( declaredDeviceState, discoveredDeviceState, state == DiffType.Added, allowUnknownDevices );

      var portDiffs = GetPortDifferences( differences, diff.PropertyPath );

      foreach ( var portDiff in portDiffs ) {
        logger?.LogTrace( "Port diff: {Action} {Path}", portDiff.DiffType, portDiff.PropertyPath );
      }

      var ports = portDiffs.Select( p => {
        return p.DiffType switch {
          DiffType.Unchanged => ( (Port) p.Original! ),
          DiffType.Removed => ( (Port) p.Original! ),
          DiffType.Added => ( (Port) p.Updated! ),
          _ => throw new Exception( "øv" )
        };
      } ).ToList();

      var ids = device.Addresses.Where( a => a.IsId.HasValue && a.IsId.Value ).Select( a => a.Type ).ToList();

      var hostname = device.Get( AddressType.Hostname );
      var mac = device.Get( AddressType.Mac );

      string[] row = [
        status,
        // Blue dot = part of device ID
        // ... or use bold? maybe plus small color difference?
        MarkId( ( false ? "[darkblue]•[/] " : "" ) + device.Get( AddressType.IpV4 ), AddressType.IpV4, ids ),
        //MarkId( ( hostname != null ? "[gray]" + hostname.ToLowerInvariant() + "[/]" : "" ), AddressType.Hostname, ids ),
        //TODO analyzer rule for culture variance
        MarkId( ( mac != null ? "[gray]" + ( FakeMac ? GenerateMacAddress() : mac.ToUpperInvariant() ) + "[/]" : "" ),
          AddressType.Mac, ids ),
        textStatus
        //string.Join( " ", ports.Select( x => x.Value ) )
      ];

      if ( showCustomIdColumn.HasValue && showCustomIdColumn.Value ) {
        var modified = row.ToList();
        modified.Insert( 2, "[gray]" + ( declaredDevice?.Id ?? "" ) + "[/]" );
        row = modified.ToArray();
      }

      table.AddRow( row );
    }
  }

  /**
   * - [green]●[/] — Online and should be online
   * - [green]○[/] — Should be offline and is offline
   * - [red]●[/] — Should be offline but is online
   * - [red]○[/] — Should be online but is offline
   * - [yellow]?[/] — Unknown device (allowed) (or undefined state???)
   * - [red]![/] — Unknown device (not allowed/disallowed)
   */
  private static string GetStatusIcon(
    DeclaredDeviceState? declared,
    DiscoveredDeviceState? discovered,
    bool isUnknown,
    bool unknownAllowed
  ) {
    // Unicode icons
    const string ClosedCircle = "\u25CF"; // ●
    const string OpenCircle = "\u25CB"; // ○
    const string QuestionMark = "\u003F"; // ?
    const string Exclamation = "\u0021"; // !
    const string ClosedDiamond = "\u25C6"; // ◆
    const string OpenDiamond = "\u25C7"; // ◇

    // Known device
    if ( !isUnknown ) {
      return declared switch {
        // expecting up, is up
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Online => $"[green]{ClosedCircle}[/]",
        // expecting up, is down
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Offline => $"[red]{OpenCircle}[/]",
        // expecting down, is up
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Online => $"[red]{ClosedCircle}[/]",
        // expecting down, is down
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Offline => $"[green]{OpenCircle}[/]",
        // expecting either, is up
        DeclaredDeviceState.Dynamic when discovered ==
                                         DiscoveredDeviceState
                                           .Online => $"[darkgreen]{ClosedDiamond}[/]", //TODO or yellow
        // expecting either, is down
        DeclaredDeviceState.Dynamic when discovered ==
                                         DiscoveredDeviceState
                                           .Offline => $"[darkgreen]{OpenDiamond}[/]", //TODO or yellow
        _ => $"[yellow][bold]{QuestionMark}[/][/]"
      };
      // State not specified/undefined (yellow)
    }

    // Unknown device
    if ( isUnknown && !unknownAllowed )
      return $"[red][bold]{Exclamation}[/][/]"; // Disallowed: red exclamation
    if ( isUnknown && unknownAllowed )
      return $"[yellow][bold]{QuestionMark}[/][/]"; // Allowed: yellow question

    // Fallback/Undefined
    return $"[yellow][bold]{QuestionMark}[/][/]";
  }

  private static string GetStatusText(
    DeclaredDeviceState? declared,
    DiscoveredDeviceState? discovered,
    bool isUnknown,
    bool unknownAllowed
  ) {
    // Known device
    if ( !isUnknown ) {
      return declared switch {
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Online => "", //"[green]Online (expected)[/]",
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Offline => "[red]Offline (unexpected)[/]",
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Online => "[red]Online (unexpected)[/]",
        DeclaredDeviceState.Down when discovered ==
                                      DiscoveredDeviceState.Offline => "", //"[green]Offline (expected)[/]",
        DeclaredDeviceState.Dynamic when discovered ==
                                         DiscoveredDeviceState.Online => "", //"[yellow]Online (allowed)[/]",
        DeclaredDeviceState.Dynamic when discovered ==
                                         DiscoveredDeviceState.Offline => "", //""[yellow]Offline (allowed)[/]",
        _ => "[yellow]State unknown or unspecified[/]"
      };
    }

    // Unknown device
    if ( isUnknown && !unknownAllowed )
      return "[red]Unknown device[/]";
    if ( isUnknown && unknownAllowed )
      return "[yellow]Unknown device[/]";

    // Fallback/Undefined
    return "[yellow]Unknown or undefined[/]";
  }


  private static string MarkId( string text, AddressType type, List<AddressType> ids ) {
    //TODO only mark if marked in spec
    if ( ids.Contains( type ) ) {
      //return "[bold]" + text + "[/]";
      return text;
    }

    //return text;
    return $"[italic][dim]{text}[/][/]";
  }


  private static Table CreateTable( bool showCustomIdColumn = false ) {
    const int padding = 1; //2 if no border
    var table = new Table();
    table.SquareBorder();
    table.AddColumn( new TableColumn( "" ).LeftAligned() );
    table.AddColumn( new TableColumn( "IP" ).LeftAligned().PadRight( padding ) );
    if ( showCustomIdColumn )
      table.AddColumn( new TableColumn( "ID" ).LeftAligned().PadRight( padding ) );
    //table.AddColumn( new TableColumn( "Hostname" ).LeftAligned().PadRight( padding ) );
    table.AddColumn( new TableColumn( "MAC" ).LeftAligned().PadRight( padding ) );
    table.AddColumn( new TableColumn( "" ).LeftAligned() );
    //table.AddColumn( new TableColumn( "Ports" ).LeftAligned() );
    return table;
  }

  private static string GenerateMacAddress() {
    var rand = new Random();
    byte[] macBytes = new byte[6];
    rand.NextBytes( macBytes );
    return string.Join( ":", macBytes.Select( b => b.ToString( "X2" ) ) );
  }
}