using System.Text.RegularExpressions;
using Drift.Cli.Renderer;
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
        .ConfigureDiffDeviceKeySelectors()
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

  protected static List<ObjectDiff> GetPortDifferences( List<ObjectDiff> differences, string propertyPath ) {
    return differences.Where( d =>
        Regex.IsMatch( d.PropertyPath, $@"^{Regex.Escape( propertyPath )}\.Ports\[[^\]]+?\]$" ) )
      .OrderBy( d => d.PropertyPath )
      .ToList();
  }

  protected abstract void Render( List<ObjectDiff> differences, IEnumerable<DeclaredDevice> declaredDevices,
    // TODO Delete ILogger parameter
    ILogger? logger = null
  );
}