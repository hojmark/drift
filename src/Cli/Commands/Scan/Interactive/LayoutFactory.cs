namespace Drift.Cli.Commands.Scan.Interactive;

using Spectre.Console;
using Spectre.Console.Rendering;

public static class LayoutFactory
{
  public static Layout Create()
  {
    var layout = new Layout("Root")
      .SplitRows(
        new Layout("Header") { Size = 1 },
        new Layout("Body"),
        new Layout("Footer") { Size = 1 }
      );

    layout["Body"].SplitRows(
      new Layout("MainPanel"),
      new Layout("Progress") { Size = 1 }
    );

    layout["Header"].Update(BuildHeader());
    layout["Progress"].Update(BuildProgressChart());

    return layout;
  }

  private static Markup BuildHeader()
  {
    return new Markup("Using [grey]/home/hojmark/[/][yellow bold]fh47[/][grey].spec.yaml[/]  [green]✔[/]");
  }

  private static BreakdownChart BuildProgressChart()
  {
    return new BreakdownChart()
      .HideTags()
      .Width(AnsiConsole.Console.Profile.Width)
      .AddItem("Good", 85, Color.Green)
      .AddItem("Unknown", 5, Color.Yellow)
      .AddItem("Bad", 2, Color.Red)
      .AddItem("Bad", 8, Color.Grey);
  }
}
