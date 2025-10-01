using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;

namespace Drift.Cli.Presentation.Rendering;

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

internal static class DeviceStateIndicator {
  // Unicode icons
  const string ClosedCircle = "\u25CF"; // ●
  const string OpenCircle = "\u25CB"; // ○
  const string ClosedDiamond = "\u25C6"; // ◆
  const string OpenDiamond = "\u25C7"; // ◇
  const string QuestionMark = "\u003F"; // ?
  const string Exclamation = "\u0021"; // !

  /**
  * - [green]●[/] — Online and should be online
  * - [green]○[/] — Should be offline and is offline
  * - [red]●[/] — Should be offline but is online
  * - [red]○[/] — Should be online but is offline
  * - [yellow]?[/] — Unknown device (allowed) (or undefined state???)
  * - [red]![/] — Unknown device (not allowed/disallowed)
  */
  internal static string GetIcon(
    DeclaredDeviceState? declared,
    DiscoveredDeviceState? discovered,
    bool isUnknown,
    bool unknownAllowed
  ) {
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
        DeclaredDeviceState.Dynamic when discovered == DiscoveredDeviceState.Online => $"[darkgreen]{ClosedDiamond}[/]",
        // expecting either, is down
        DeclaredDeviceState.Dynamic when discovered == DiscoveredDeviceState.Offline => $"[darkgreen]{OpenDiamond}[/]",
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
    return $"[purple][bold]{QuestionMark}[/][/]";
  }

  internal static string GetText(
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
        DeclaredDeviceState.Down when discovered == DiscoveredDeviceState.Offline => onlyDrifted
          ? ""
          : "[green]Offline[/]",
        DeclaredDeviceState.Dynamic when discovered == DiscoveredDeviceState.Online => onlyDrifted
          ? ""
          : "[green]Online[/]",
        DeclaredDeviceState.Dynamic when discovered == DiscoveredDeviceState.Offline => onlyDrifted
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
    return "[purple]Undefined[/]";
  }
}