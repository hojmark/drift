using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;

namespace Drift.Cli.Presentation.Rendering.DeviceState;

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

/**
* - [green]●[/] — Online and should be online
* - [green]○[/] — Should be offline and is offline
* - [red]●[/] — Should be offline but is online
* - [red]○[/] — Should be online but is offline
* - [yellow]?[/] — Unknown device (allowed) (or undefined state???)
* - [red]![/] — Unknown device (not allowed/disallowed)
*/
internal class DeviceRenderState( DeviceState state, string icon, string text ) {
  public DeviceState State {
    get;
  } = state;

  public string Icon {
    get;
  } = icon;

  public string Text {
    get;
  } = text;

  // Unicode icons
  //TODO move to chars?
  const string ClosedCircle = "\u25CF"; // ●
  const string OpenCircle = "\u25CB"; // ○
  const string ClosedDiamond = "\u25C6"; // ◆
  const string OpenDiamond = "\u25C7"; // ◇
  const string QuestionMark = "\u003F"; // ?
  const string Exclamation = "\u0021"; // !

  internal static DeviceRenderState From(
    DeclaredDeviceState? declared,
    DiscoveredDeviceState discovered,
    bool unknownAllowed
  ) {
    var state = DeviceStateResolver.Get( declared, discovered, unknownAllowed );
    return From( state );
  }

  internal static DeviceRenderState From( DeviceState state ) {
    return state switch {
      DeviceState.Undefined => new DeviceRenderState(
        state,
        $"[purple bold]{QuestionMark}[/]",
        "[purple]Undefined[/]"
      ),
      DeviceState.KnownExpectedOnline => new DeviceRenderState(
        state,
        $"[green]{ClosedCircle}[/]",
        "[green]Online[/]"
      ),
      DeviceState.KnownExpectedOffline => new DeviceRenderState(
        state,
        $"[green]{OpenCircle}[/]",
        "[green]Offline[/]"
      ),
      DeviceState.KnownUnexpectedOnline => new DeviceRenderState(
        state,
        $"[red]{ClosedCircle}[/]",
        "[red]Online[/]"
      ),
      DeviceState.KnownUnexpectedOffline => new DeviceRenderState(
        state,
        $"[red]{OpenCircle}[/]",
        "[red]Offline[/]"
      ),
      DeviceState.KnownDynamicOnline => new DeviceRenderState(
        state,
        $"[darkgreen]{ClosedDiamond}[/]",
        "[green]Online[/]"
      ),
      DeviceState.KnownDynamicOffline => new DeviceRenderState(
        state,
        $"[darkgreen]{OpenDiamond}[/]",
        "[green]Offline[/]"
      ),
      DeviceState.UnknownAllowed => new DeviceRenderState(
        state,
        $"[yellow bold]{QuestionMark}[/]",
        "[yellow]Online (unknown device)[/]"
      ),
      DeviceState.UnknownDisallowed => new DeviceRenderState(
        state,
        $"[red bold]{Exclamation}[/]",
        "[red]Online (unknown device)[/]"
      ),
      _ => throw new ArgumentOutOfRangeException( nameof(state), state, null )
    };
  }
}