using Drift.Cli.Commands.Scan.Interactive;
using Drift.Cli.Commands.Scan.Interactive.Input;

namespace Drift.Cli.Tests;

internal sealed class KeyMapTests {
  [Test]
  public void AllUiActions_ShouldBeMapped_ByDefaultKeymap() {
    var keymap = new DefaultKeyMap();
    var allConsoleKeys = Enum.GetValues<ConsoleKey>().Cast<ConsoleKey>();

    var mappedActions = allConsoleKeys
      .Select( key => keymap.Map( key ) )
      .Where( action => action != UiAction.None )
      .Distinct()
      .ToHashSet();

    var requiredActions = Enum.GetValues<UiAction>()
      .Where( a => a != UiAction.None )
      .ToHashSet();

    var missing = requiredActions.Except( mappedActions ).ToList();
    Assert.That( missing, Has.Count.EqualTo( 0 ), $"Unmapped {nameof(UiAction)}: {string.Join( ", ", missing )}" );
  }
}