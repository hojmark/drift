using Drift.Cli.Output.Abstractions;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Rendering;

//TODO use NormalOutput instead of Spectre directly
internal class NormalScanRenderer( INormalOutput console ) : DiffRendererBase {
  // Anonymising MACs e.g. for GitHub screenshot
  //TODO replace with more generic data simulation
  private const bool FakeMac = false;
  internal static IdMarkingStyle IdMarkingStyle = IdMarkingStyle.Text;

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
    var showHostnameColumn = declaredDevices.Any( d => d.Addresses.Any( a => a.Type == AddressType.Hostname ) );
    var table = CreateTable( showCustomIdColumn, showHostnameColumn );

    AddDevices( table, differences, declaredDevices, showCustomIdColumn, showHostnameColumn, logger );

    console.GetAnsiConsole().Write( table );

    //console.WriteLine( "Discovered: 12 devices • Online: 2 • Offline: 8 • Unknown: 2");

    //console.WriteLine( $"Total declared devices: {data.DevicesDeclared.Count()}" ); //TODO host vs device terminology
    //console.WriteLine( $"Total discovered devices: {data.DevicesDiscovered.Count()}" );
  }

  private static void AddDevices( Table table,
    List<ObjectDiff> differences,
    IEnumerable<DeclaredDevice> declaredDevices,
    bool? showCustomIdColumn = false,
    bool? showHostnameColumn = false,
    ILogger? logger = null
  ) {
    var directDeviceDifferences = GetDirectDeviceDifferences( differences );

    var rows = new List<string[]>();

    foreach ( var diff in directDeviceDifferences ) {
      logger?.LogTrace( "Device diff: {Action} {Path}", diff.DiffType, diff.PropertyPath );
      // Console.WriteLine( $"{diff.DiffType + ":",-10} {diff.PropertyPath}" );

      var state = diff.DiffType;

      IAddressableDevice device = state switch {
        // Note: may be unchanged based on device id, but other value may be updated in which case we'd like to show the updated values... but this is debatable... maybe both or a merge should be shown
        DiffType.Unchanged => ( (DiffDevice) diff.Updated! ),
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

      var declaredDeviceMultiple = declaredDevices.Where( d =>
        ( (IAddressableDevice) d ).GetDeviceId() == device.GetDeviceId()
      ).ToList();

      if ( declaredDeviceMultiple.Count > 1 ) {
        // TODO review error message
        throw new Exception( "Multiple declared devices have the same ID: " +
                             string.Join( ", ", declaredDeviceMultiple.Select( d => d.Id ) )
        );
      }

      var declaredDevice = declaredDeviceMultiple.SingleOrDefault();
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

      var mac = device.Get( AddressType.Mac );

      var deviceId = device.GetDeviceId();
      var deviceIdDeclared = ( declaredDevice as IAddressableDevice )?.GetDeviceId();

      string[] row = [
        status,
        MarkId( device.Get( AddressType.IpV4 ) ?? "", AddressType.IpV4, deviceIdDeclared ),
        MarkId(
          (
            mac != null
              ? ( FakeMac ? GenerateMacAddress() : mac.ToUpperInvariant() )
              : ""
          ), AddressType.Mac, deviceIdDeclared
        ),
        textStatus
        //string.Join( " ", ports.Select( x => x.Value ) )
      ];

      if ( showHostnameColumn.HasValue && showHostnameColumn.Value ) {
        var hostnameAsString = device.Get( AddressType.Hostname )?.ToLowerInvariant();
        var modified = row.ToList();
        //TODO enable analyzer rule for culture variance
        modified.Insert( 2, MarkId( hostnameAsString ?? "", AddressType.Hostname, deviceIdDeclared ) );
        row = modified.ToArray();
      }

      if ( showCustomIdColumn.HasValue && showCustomIdColumn.Value ) {
        var modified = row.ToList();
        modified.Insert( 2, "[gray]" + ( declaredDevice?.Id ?? "" ) + "[/]" );
        //modified.Insert( 2, "[gray]" + ( ( declaredDevice as IAddressableDevice )?.GetDeviceId() ) + "[/]" );
        row = modified.ToArray();
      }

      rows.Add( row );
    }

    // Order by IP
    // TODO hack, make dynamic
    foreach ( var row in rows.OrderBy( row =>
                 row[1]
                   .RemoveMarkup()
                   .Replace( ".", "" ),
               StringComparer.OrdinalIgnoreCase.WithNaturalSort() ) ) {
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


  private static string MarkId( string text, AddressType type, DeviceId? idDeclared ) {
    if ( idDeclared != null && idDeclared.Contributes( type ) ) {
      //return "[bold]" + text + "[/]";
      return IdMarkingStyle switch {
        IdMarkingStyle.Text => text,
        IdMarkingStyle.Dot => $"{text} [blue]•[/]", // ◦•
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    return idDeclared == null
      ? $"[gray]{text}[/]" // TODO use yellow?
      : $"[gray]{text}[/]";
  }


  private static Table CreateTable( bool showCustomIdColumn = false, bool showHostnameColumn = true ) {
    const int padding = 1; //2 if no border
    var table = new Table();
    table.SquareBorder();
    table.AddColumn( new TableColumn( "" ).LeftAligned() );
    table.AddColumn( new TableColumn( "IP" ).LeftAligned().PadRight( padding ) );
    if ( showCustomIdColumn )
      table.AddColumn( new TableColumn( "ID" ).LeftAligned().PadRight( padding ) );
    if ( showHostnameColumn )
      table.AddColumn( new TableColumn( "Hostname" ).LeftAligned().PadRight( padding ) );
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