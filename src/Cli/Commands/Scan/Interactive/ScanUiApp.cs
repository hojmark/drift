using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

public class ScanUiApp {
  private readonly List<Subnet> _subnets;
  private readonly bool[] _expanded;
  private readonly Layout _layout;
  private int _selectedIndex = 0;
  private int _scrollOffset = 0;
  private bool _running = true;

  private readonly TreeRenderer _renderer;
  private readonly InputHandler _input;

  public ScanUiApp(List<Subnet> subnets) {
    _subnets = subnets;
    _expanded = Enumerable.Repeat(true, subnets.Count).ToArray();
    _layout = LayoutFactory.Create();
    _renderer = new TreeRenderer(_subnets, _expanded);
    _input = new InputHandler(_subnets.Count);
  }

  public void Run() {
    AnsiConsole.Live(_layout).Start(ctx => {
      while (_running) {
        Render(ctx);
        HandleInput();
        Thread.Sleep(50);
      }
    });

    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold green]Exited.[/]");
  }

  private void Render(LiveDisplayContext ctx) {
    int availableRows = GetAvailableRows();
    int maxScroll = Math.Max(0, _renderer.GetTotalHeight() - availableRows);
    _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

    var trees = _renderer.RenderTrees(_scrollOffset, availableRows, _selectedIndex);

    _layout["MainPanel"].Update(
      new Panel(new Rows(trees)).Expand().Border(BoxBorder.Square).Padding(0, 0)
    );

    _layout["Footer"].Update(BuildFooter(_scrollOffset, maxScroll, _selectedIndex));

    ctx.Refresh();
  }

  private void HandleInput() {
    if (!Console.KeyAvailable) return;

    var key = Console.ReadKey(true).Key;
    var action = InputHandler.MapKey(key);

    switch (action) {
      case InputAction.Quit:
        _running = false;
        break;
      case InputAction.ScrollUp:
        _scrollOffset -= TreeRenderer.ScrollAmount;
        break;
      case InputAction.ScrollDown:
        _scrollOffset += TreeRenderer.ScrollAmount;
        break;
      case InputAction.MoveUp:
        _selectedIndex = Math.Max(0, _selectedIndex - 1);
        break;
      case InputAction.MoveDown:
        _selectedIndex = Math.Min(_subnets.Count - 1, _selectedIndex + 1);
        break;
      case InputAction.Expand:
        _expanded[_selectedIndex] = true;
        break;
      case InputAction.Collapse:
        _expanded[_selectedIndex] = false;
        break;
      case InputAction.ToggleSelected:
        _expanded[_selectedIndex] = !_expanded[_selectedIndex];
        break;
      case InputAction.ToggleByIndex:
        int idx = _input.GetNumericIndex(key);
        if (idx < _subnets.Count) _expanded[idx] = !_expanded[idx];
        break;
    }
  }
  
  public Markup BuildFooter(int scroll, int maxScroll, int selectedIndex)
    => new Markup(
      $"[green]q[/] quit   [green]↑/↓[/] navigate   [green]←/→[/] toggle   " +
      $"[green]w/s[/] scroll   [grey]Scroll: {scroll}/{maxScroll}[/]  " +
      $"[grey]Selected: {selectedIndex + 1}/{_subnets.Count}[/]"
    );

  private int GetAvailableRows()
    => AnsiConsole.Console.Profile.Height - 1 - 1 - 1 - 2; // header + footer + progress + padding
}
