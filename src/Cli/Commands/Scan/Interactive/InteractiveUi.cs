using System.Text.RegularExpressions;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Scan.Interactive.KeyMaps;
using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Cli.Commands.Scan.Rendering;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Output.Logging;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using NaturalSort.Extension;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

// TODO keymaps: default, vim, emacs, etc.
// TODO themes: nocolor i.e. black/white, monochrome, default, etc.
internal class InteractiveUi : IAsyncDisposable {
  private const int ScrollAmount = 1;

  // Anonymising MACs e.g. for GitHub screenshot
  //TODO replace with more generic data simulation
  private const bool FakeMac = false;

  private readonly IOutputManager _outputManager;
  private readonly Network? _network;
  private readonly INetworkScanner _scanner;
  private readonly ScanLayout _layout;

  private readonly CancellationTokenSource _running = new();

  //TODO should get IDisposable warning?
  private readonly KeyWatcher _keyWatcher = new();
  private readonly ConsoleResizeWatcher _resizeWatcher = new();

  private readonly List<Subnet> _subnets = [];
  private readonly NetworkScanOptions _scanRequest;
  private readonly IKeyMap _keyMap;

  private Percentage _progress = Percentage.Zero;

  private readonly ILogReader _logReader;
  private TaskCompletionSource _logUpdateSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private readonly LogView _logView;

  private TaskCompletionSource _scanUpdateSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

  public InteractiveUi(
    IOutputManager outputManager,
    Network? network,
    INetworkScanner scanner,
    NetworkScanOptions scanRequest,
    IKeyMap keyMap,
    bool initialLogExpansion
  ) {
    _scanner = scanner;
    _scanRequest = scanRequest;
    _keyMap = keyMap;
    _layout = new ScanLayout( network?.Id );
    _outputManager = outputManager;
    _network = network;
    _scanner.ResultUpdated += OnScanResultUpdated;

    _logReader = new LogReader( _outputManager );
    _logReader.LogUpdated += OnLogUpdated;

    SubnetView = new SubnetView( () => _layout.AvailableRows );
    _logView = new LogView( () => _layout.AvailableRows );
    _layout.ShowLogs = initialLogExpansion;
  }

  private SubnetView SubnetView {
    get;
    set;
  }

  public async Task<int> RunAsync() {
    await AnsiConsole
      .Live( _layout.Renderable )
      .AutoClear( true )
      .StartAsync( async ctx => {
          await RenderAsync();
          ctx.Refresh();

          // TODO Async scan and log reading started using different patterns
          _ = StartScanAsync();
          await _logReader.StartAsync( _running.Token );

          while ( !_running.IsCancellationRequested ) {
            var keyTask = _keyWatcher.WaitForKeyAsync();
            var resizeTask = _resizeWatcher.WaitForResizeAsync();
            var logTask = _logUpdateSignal.Task;
            var scanTask = _scanUpdateSignal.Task;

            await Task.WhenAny( keyTask, logTask, scanTask, resizeTask );

            ProcessInput();
            await RenderAsync();
            ctx.Refresh();
          }
        }
      );

    return ExitCodes.Success;
  }

  private void ProcessInput() {
    var key = _keyWatcher.Consume();
    if ( key != null ) {
      var action = _keyMap.Map( key.Value );
      Handle( action );
    }
  }

  private Task<NetworkScanResult> StartScanAsync() {
    return _scanner.ScanAsync( _scanRequest, _outputManager.GetLogger(), _running.Token );
  }

  private void OnLogUpdated( object? sender, string line ) {
    _logView.AddLine( line );
    _logUpdateSignal.TrySetResult();
    _logUpdateSignal = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
  }

  private async Task RenderAsync() {
    SubnetView.Subnets = _subnets;
    _layout.SetDebug( SubnetView.DebugData );
    _layout.SetLog( _logView );
    _layout.SetScanTree( SubnetView );
    _layout.SetProgress( _progress );
  }

  private void Handle( UiAction action ) {
    switch ( action ) {
      case UiAction.Quit:
        _running.Cancel();
        break;
      case UiAction.ScrollUp:
        SubnetView.ScrollOffset -= ScrollAmount;
        //TODO should be separately scrollable
        _logView.ScrollOffset -= ScrollAmount;
        break;
      case UiAction.ScrollDown:
        SubnetView.ScrollOffset += ScrollAmount;
        //TODO should be separately scrollable
        _logView.ScrollOffset += ScrollAmount;
        break;
      case UiAction.ScrollUpPage:
        SubnetView.ScrollOffset -= (int) _layout.AvailableRows;
        _logView.ScrollOffset -= (int) _layout.AvailableRows;
        break;
      case UiAction.ScrollDownPage:
        SubnetView.ScrollOffset += (int) _layout.AvailableRows;
        _logView.ScrollOffset += (int) _layout.AvailableRows;
        break;
      case UiAction.MoveUp:
        SubnetView.SelectPrevious();
        break;
      case UiAction.MoveDown:
        SubnetView.SelectNext();
        break;
      case UiAction.ToggleSubnet:
        SubnetView.ToggleSelected();
        break;
      case UiAction.RestartScan:
        _subnets.Clear();
        StartScanAsync();
        break;
      case UiAction.ToggleLog:
        _layout.ShowLogs = !_layout.ShowLogs;
        break;
      case UiAction.ToggleDebug:
        _layout.ShowDebug = !_layout.ShowDebug;
        break;
      case UiAction.None:
      default:
        break;
    }
  }

  private void OnScanResultUpdated( object? sender, NetworkScanResult scanResult ) {
    lock ( _subnets ) {
      _progress = scanResult.Progress;

      var original = _network == null ? [] : _network.Devices.Where( d => d.IsEnabled() ).ToList();
      var declaredDevices = original;
      var updated1 = scanResult.Subnets.First().DiscoveredDevices;

      var differences = ObjectDiffEngine.Compare(
        original.ToDiffDevices(),
        updated1.ToDiffDevices(),
        "Device",
        new DiffOptions()
          .ConfigureDiffDeviceKeySelectors( original.ToList() )
          // Includes Unchanged, which makes for an easier table population
          .SetDiffTypesAll()
        //, logger //TODO support ioutputmanager or create ilogger adapter?
      );

      var directDeviceDifferences = GetDirectDeviceDifferences( differences );

      var devices = new List<Device>();

      foreach ( var diff in directDeviceDifferences ) {
        //logger?.LogTrace( "Device diff: {Action} {Path}", diff.DiffType, diff.PropertyPath );
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
          GetStatusText( declaredDeviceState, discoveredDeviceState, state == DiffType.Added, allowUnknownDevices,
            onlyDrifted: false );

        var mac = device.Get( AddressType.Mac );

        var deviceId = device.GetDeviceId();
        var deviceIdDeclared = ( declaredDevice as IAddressableDevice )?.GetDeviceId();

        var d = new Device {
          State = status,
          IpRaw = device.Get( AddressType.IpV4 ) ?? "",
          Ip = MarkId( device.Get( AddressType.IpV4 ) ?? "", AddressType.IpV4, deviceIdDeclared ),
          MacRaw = mac != null
            ? ( FakeMac ? GenerateMacAddress() : mac.ToUpperInvariant() )
            : "",
          Mac = MarkId(
            (
              mac != null
                ? ( FakeMac ? GenerateMacAddress() : mac.ToUpperInvariant() )
                : ""
            ), AddressType.Mac, deviceIdDeclared
          ),
          IdRaw = declaredDevice?.Id ?? "",
          Id = "[grey]" + ( declaredDevice?.Id ?? "" ) + "[/]",
          StateText = textStatus
        };

        devices.Add( d );
      }

      // Order by IP
      devices = devices.OrderBy( dev => dev.IpRaw, StringComparer.OrdinalIgnoreCase.WithNaturalSort() ).ToList();

      var currentSubnets = scanResult.Subnets
        .Select( subnet => new Subnet {
          Cidr = subnet.CidrBlock, Devices = devices

          /*subnet.DiscoveredDevices.Select( device =>
          new Device {
            Ip = device.Get( AddressType.IpV4 ) ?? "n/a",
            Mac = device.Get( AddressType.Mac ) ?? "n/a",
            Hostname = device.Get( AddressType.Mac ) ?? "n/a"
          } ).ToList()*/
        } ).ToList();

      var existing = _subnets.ToDictionary( s => s.Cidr );
      var updated = new List<Subnet>();

      foreach ( var subnet in currentSubnets ) {
        if ( existing.TryGetValue( subnet.Cidr, out var existingSubnet ) ) {
          subnet.IsExpanded = existingSubnet.IsExpanded;
        }

        updated.Add( subnet );
      }

      _subnets.Clear();
      _subnets.AddRange( updated );
      _scanUpdateSignal.TrySetResult();
      _scanUpdateSignal = new TaskCompletionSource( TaskCreationOptions.RunContinuationsAsynchronously );
    }
  }

  private static List<ObjectDiff> GetDirectDeviceDifferences( List<ObjectDiff> differences ) {
    return differences
      .Where( d => Regex.IsMatch( d.PropertyPath, @"^Device\[[^\]]+?\]$" ) )
      .OrderBy( d => d.PropertyPath, StringComparison.OrdinalIgnoreCase.WithNaturalSort() )
      .ToList();
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
    bool unknownAllowed,
    bool onlyDrifted = true
  ) {
    // Known device
    if ( !isUnknown ) {
      return declared switch {
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Online => onlyDrifted
          ? ""
          : "[green]Online[/]",
        DeclaredDeviceState.Up when discovered == DiscoveredDeviceState.Offline => "[red]Offline[/]",
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Online => "[red]Online[/]",
        DeclaredDeviceState.Down when discovered ==
                                      DiscoveredDeviceState.Offline =>
          onlyDrifted ? "" : "[green]Offline[/]",
        DeclaredDeviceState.Dynamic when discovered ==
                                         DiscoveredDeviceState.Online => onlyDrifted
          ? ""
          : "[green]Online[/]",
        DeclaredDeviceState.Dynamic when discovered ==
                                         DiscoveredDeviceState.Offline => onlyDrifted
          ? ""
          : "[green]Offline[/]",
        _ => "[yellow]State unknown or unspecified[/]"
      };
    }

    // Unknown device
    if ( isUnknown && !unknownAllowed )
      return "[red]Online (unknown device)[/]";
    if ( isUnknown && unknownAllowed )
      return "[yellow]Online (unknown device)[/]";

    // Fallback/Undefined
    return "[yellow]Unknown or undefined[/]";
  }


  internal static IdMarkingStyle IdMarkingStyle = IdMarkingStyle.Text;

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

  private static string GenerateMacAddress() {
    var rand = new Random();
    byte[] macBytes = new byte[6];
    rand.NextBytes( macBytes );
    return string.Join( ":", macBytes.Select( b => b.ToString( "X2" ) ) );
  }

  public async ValueTask DisposeAsync() {
    _scanner.ResultUpdated -= OnScanResultUpdated;
    _running.Dispose();
    await _keyWatcher.DisposeAsync();
    _logReader.LogUpdated -= OnLogUpdated;
  }
}