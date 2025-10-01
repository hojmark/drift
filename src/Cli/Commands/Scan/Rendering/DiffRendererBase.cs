using System.Text.RegularExpressions;
using Drift.Cli.Presentation.Rendering;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain.Device.Declared;
using Microsoft.Extensions.Logging;
using NaturalSort.Extension;

namespace Drift.Cli.Commands.Scan.Rendering;

internal abstract class DiffRendererBase : IRenderer<ScanRenderData> {
  public void Render( ScanRenderData data ) {
    var differences = ObjectDiffEngine.Compare(
      original: data.DevicesDeclared.Where( d => d.Enabled ?? true ).ToDiffDevices(),
      updated: data.DevicesDiscovered.ToDiffDevices(),
      "Device",
      new DiffOptions()
        .ConfigureDiffDeviceKeySelectors( data.DevicesDeclared.ToList() )
        // Includes Unchanged, which makes for an easier table population
        .SetDiffTypesAll()
      //, logger //TODO support ioutputmanager or create ilogger adapter?
    );

    Render( differences, data.DevicesDeclared );
  }

  // TODO direct?
  protected static List<ObjectDiff> GetDirectDeviceDifferences( List<ObjectDiff> differences ) {
    return differences
      .Where( d => Regex.IsMatch( d.PropertyPath, @"^Device\[[^\]]+?\]$" ) )
      .OrderBy( d => d.PropertyPath, StringComparison.OrdinalIgnoreCase.WithNaturalSort() )
      .ToList();
  }

  protected abstract void Render( List<ObjectDiff> differences, IEnumerable<DeclaredDevice> declaredDevices,
    // TODO Delete ILogger parameter
    ILogger? logger = null
  );
}