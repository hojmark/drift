using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Cli.Commands.Scan.Interactive.Ui;
using Drift.Cli.Output.Abstractions;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class NormalScanRendererNeo( INormalOutput console ) {
  public void Render( List<Subnet> subnets ) {
    var trees = TreeRenderer.Render( subnets, null, 100000, 0, showAccordionSymbols: false );
    foreach ( var tree in trees ) {
      console.GetAnsiConsole().Write( tree );
    }
  }
}