using Drift.Cli.Commands.Scan.Interactive.Ui;
using Drift.Cli.Commands.Scan.Models;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class NormalScanRenderer( INormalOutput console ) {
  public void Render( List<Subnet> subnets ) {
    //console.GetAnsiConsole().Write( new Rule() );
    var trees = TreeRenderer.Render( subnets, null, 100000, 0, showAccordionSymbols: false );
    foreach ( var tree in trees ) {
      console.GetAnsiConsole().Write( tree );
      console.GetAnsiConsole().WriteLine();
    }
  }
}