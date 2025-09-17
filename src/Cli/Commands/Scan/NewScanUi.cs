using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Drift.Cli.Commands.Scan.New;

public static class NewScanUi {
  private const int HeaderRows = 1;
  private const int FooterRows = 1;
  private const int ProgressRows = 1;
  private const int ScrollAmount = 3;

  internal static void Show() {
    var subnets = GetSampleSubnets();

    int treeCount = subnets.Count;
    bool[] expanded = Enumerable.Repeat(true, treeCount).ToArray();
    int scrollRowOffset = 0;
    int selectedIndex = 0;
    bool running = true;

    int ipWidth = subnets.SelectMany(s => s.Devices).Max(d => d.IP.Length);
    int macWidth = subnets.SelectMany(s => s.Devices).Max(d => d.MAC.Length);
    int statusWidth = "Offline".Length;

    var layout = CreateLayout();

    AnsiConsole.Live(layout).Start(ctx => {
      while (running) {
        int terminalHeight = AnsiConsole.Console.Profile.Height;
        int availableRows = terminalHeight - HeaderRows - FooterRows - ProgressRows - 2;

        int totalTreeRows = CalculateTotalTreeRows(subnets, expanded);
        int maxScrollOffset = Math.Max(0, totalTreeRows - availableRows);
        scrollRowOffset = Math.Clamp(scrollRowOffset, 0, maxScrollOffset);

        var treesToRender = BuildTreesToRender(subnets, expanded, scrollRowOffset, availableRows, selectedIndex, ipWidth, macWidth, statusWidth);

        layout["MainPanel"].Update(
          new Panel(new Rows(treesToRender.Select(t => t.tree)))
            .Expand()
            .Border(BoxBorder.Square)
            .Padding(0, 0)
        );

        layout["Footer"].Update(BuildFooterMarkup(scrollRowOffset, maxScrollOffset, selectedIndex, treeCount));
        ctx.Refresh();

        HandleInput(ref running, ref scrollRowOffset, ref selectedIndex, expanded, treeCount);
        Thread.Sleep(50);
      }
    });

    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold green]Exited.[/]");
  }

  // -- Tree & UI Building --

  private static List<(int index, Tree tree, int height)> BuildTreesToRender(
    List<Subnet> subnets, bool[] expanded, int scrollOffset, int maxRows, int selectedIndex,
    int ipWidth, int macWidth, int statusWidth
  ) {
    var treesToRender = new List<(int index, Tree tree, int height)>();

    int treeCount = subnets.Count;
    int skippedRows = 0;
    int startTreeIndex = 0;

    // Skip trees before scroll offset
    for (; startTreeIndex < treeCount; startTreeIndex++) {
      int treeHeight = GetTreeHeight(startTreeIndex, expanded, subnets);
      if (skippedRows + treeHeight > scrollOffset) break;
      skippedRows += treeHeight;
    }

    // Render visible trees
    int usedRows = 0;
    for (int i = startTreeIndex; i < treeCount; i++) {
      int treeHeight = GetTreeHeight(i, expanded, subnets);
      if (usedRows + treeHeight > maxRows) break;

      var isSelected = i == selectedIndex;
      var tree = CreateTree(subnets[i], expanded[i], isSelected, ipWidth, macWidth, statusWidth);
      treesToRender.Add((i, tree, treeHeight));
      usedRows += treeHeight;
    }

    return treesToRender;
  }

  private static Tree CreateTree(Subnet subnet, bool isExpanded, bool isSelected, int ipWidth, int macWidth, int statusWidth) {
    var symbol = isExpanded ? "▾" : "▸";
    var summary = isExpanded ? "" : $" [grey]({subnet.Devices.Count} devices: {subnet.Devices.Count(d => d.IsOnline)} online, {subnet.Devices.Count(d => !d.IsOnline)} offline)[/]";
    var headerContent = $"{symbol} {subnet.Address}{summary}";
    var header = isSelected
      ? $"[black on yellow]{headerContent}[/]"
      : $"[teal]{headerContent}[/]";

    var tree = new Tree(header).Guide(TreeGuide.Line);

    if (isExpanded) {
      foreach (var device in subnet.Devices) {
        var statusColor = device.IsOnline ? "green" : "red";
        var statusText = device.IsOnline ? "Online" : "Offline";

        string ip = device.IP.PadRight(ipWidth);
        string mac = device.MAC.PadRight(macWidth);
        string status = statusText.PadRight(statusWidth);

        string line = $"[white]{ip}[/]  [grey]{mac}[/]  [{statusColor}]{status}[/]";
        tree.AddNode(line);
      }
    }

    return tree;
  }

  private static int GetTreeHeight(int index, bool[] expanded, List<Subnet> subnets)
    => expanded[index] ? 1 + subnets[index].Devices.Count : 1;

  private static int CalculateTotalTreeRows(List<Subnet> subnets, bool[] expanded)
    => subnets.Select((_, i) => GetTreeHeight(i, expanded, subnets)).Sum();

  // -- Input Handling --

  private static void HandleInput(
    ref bool running, ref int scrollRowOffset, ref int selectedIndex, bool[] expanded, int treeCount
  ) {
    if (!Console.KeyAvailable) return;

    var key = Console.ReadKey(true).Key;

    switch (key) {
      case ConsoleKey.Q:
        running = false;
        break;

      case ConsoleKey.W:
        scrollRowOffset -= ScrollAmount;
        break;

      case ConsoleKey.S:
        scrollRowOffset += ScrollAmount;
        break;

      case ConsoleKey.UpArrow:
        selectedIndex = Math.Max(0, selectedIndex - 1);
        break;

      case ConsoleKey.DownArrow:
        selectedIndex = Math.Min(treeCount - 1, selectedIndex + 1);
        break;

      case ConsoleKey.LeftArrow:
        expanded[selectedIndex] = false;
        break;

      case ConsoleKey.RightArrow:
        expanded[selectedIndex] = true;
        break;

      case >= ConsoleKey.D1 and <= ConsoleKey.D9:
        int index = (int)key - (int)ConsoleKey.D1;
        if (index < treeCount) {
          expanded[index] = !expanded[index];
        }
        break;
    }
  }

  // -- Layout --

  private static Layout CreateLayout() {
    var layout = new Layout("Root")
      .SplitRows(
        new Layout("Header") { Size = HeaderRows },
        new Layout("Body"),
        new Layout("Footer") { Size = FooterRows }
      );

    layout["Body"].SplitRows(
      new Layout("MainPanel"),
      new Layout("Progress") { Size = ProgressRows }
    );

    layout["Header"].Update(
      new Markup("Using [grey]/home/hojmark/[/][yellow bold]fh47[/][grey].spec.yaml[/]  [green]✔[/]")
    );

    var chart = new BreakdownChart()
      .HideTags()
      .Width(AnsiConsole.Console.Profile.Width)
      .AddItem("Good", 85, Color.Green)
      .AddItem("Unknown", 5, Color.Yellow)
      .AddItem("Bad", 2, Color.Red)
      .AddItem("Bad", 8, Color.Grey);

    layout["Progress"].Update(chart);
    return layout;
  }

  private static Markup BuildFooterMarkup(int scrollOffset, int maxOffset, int selected, int total) {
    return new Markup(
      $"[green]q[/] quit   [green]↑/↓[/] navigate   [green]←/→[/] toggle   " +
      $"[green]w/s[/] scroll   [grey]Scroll: {scrollOffset}/{maxOffset}[/]  " +
      $"[grey]Selected: {selected + 1}/{total}[/]"
    );
  }

  // -- Sample Data --

  private static List<Subnet> GetSampleSubnets() => new() {
    new Subnet("192.168.1.0/24", new() {
      new Device("192.168.1.10", "AA:BB:CC:DD:EE:01", true),
      new Device("192.168.1.11", "AA:BB:CC:DD:EE:02", false),
      new Device("192.168.1.12", "AA:BB:CC:DD:EE:03", true),
    }),
    new Subnet("10.0.0.0/24", new() {
      new Device("10.0.0.1", "FF:EE:DD:CC:BB:01", true),
      new Device("10.0.0.2", "FF:EE:DD:CC:BB:02", true),
    }),
  };

  // -- Models --

  public class Subnet {
    public string Address { get; }
    public List<Device> Devices { get; }
    public Subnet(string address, List<Device> devices) {
      Address = address;
      Devices = devices;
    }
  }

  public class Device {
    public string IP { get; }
    public string MAC { get; }
    public bool IsOnline { get; }
    public Device(string ip, string mac, bool isOnline) {
      IP = ip;
      MAC = mac;
      IsOnline = isOnline;
    }
  }
}
