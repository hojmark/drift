using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Interactive;

public class TreeRenderer {
  public const int ScrollAmount = 3;

  private readonly List<Subnet> _subnets;
  private readonly bool[] _expanded;
  private readonly int _ipWidth;
  private readonly int _macWidth;
  private readonly int _statusWidth = "Offline".Length;

  public TreeRenderer(List<Subnet> subnets, bool[] expanded) {
    _subnets = subnets;
    _expanded = expanded;

    _ipWidth = _subnets.SelectMany(s => s.Devices).Max(d => d.IP.Length);
    _macWidth = _subnets.SelectMany(s => s.Devices).Max(d => d.MAC.Length);
  }

  public int GetTotalHeight()
    => _subnets.Select((_, i) => GetTreeHeight(i)).Sum();

  public IEnumerable<Tree> RenderTrees(int scrollOffset, int maxRows, int selectedIndex) {
    var trees = new List<Tree>();
    int usedRows = 0, skippedRows = 0, startIndex = 0;

    // Skip hidden rows
    for (; startIndex < _subnets.Count; startIndex++) {
      int height = GetTreeHeight(startIndex);
      if (skippedRows + height > scrollOffset) break;
      skippedRows += height;
    }

    for (int i = startIndex; i < _subnets.Count; i++) {
      int height = GetTreeHeight(i);
      if (usedRows + height > maxRows) break;

      var tree = BuildTree(i, i == selectedIndex);
      trees.Add(tree);
      usedRows += height;
    }

    return trees;
  }



  private Tree BuildTree(int index, bool isSelected) {
    var subnet = _subnets[index];
    var symbol = _expanded[index] ? "▾" : "▸";
    string summary = !_expanded[index]
      ? $" [grey]({subnet.Devices.Count} devices: {subnet.Devices.Count(d => d.IsOnline)} online, {subnet.Devices.Count(d => !d.IsOnline)} offline)[/]"
      : "";

    var header = $"{symbol} {subnet.Address}{summary}";
    var formattedHeader = isSelected
      ? $"[black on yellow]{header}[/]"
      : $"[teal]{header}[/]";

    var tree = new Tree(formattedHeader).Guide(TreeGuide.Line);

    if (_expanded[index]) {
      foreach (var device in subnet.Devices) {
        string statusColor = device.IsOnline ? "green" : "red";
        string statusText = device.IsOnline ? "Online" : "Offline";

        string line = $"[white]{device.IP.PadRight(_ipWidth)}[/]  [grey]{device.MAC.PadRight(_macWidth)}[/]  [{statusColor}]{statusText.PadRight(_statusWidth)}[/]";
        tree.AddNode(line);
      }
    }

    return tree;
  }

  private int GetTreeHeight(int index)
    => _expanded[index] ? 1 + _subnets[index].Devices.Count : 1;
}
