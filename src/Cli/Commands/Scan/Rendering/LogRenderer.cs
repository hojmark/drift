using Drift.Cli.Output.Abstractions;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Extensions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class LogRenderer( ILogOutput log ) : DiffRendererBase {
  protected override void Render(
    List<ObjectDiff> differences,
    IEnumerable<DeclaredDevice> declaredDevices,
    ILogger? logger = null
  ) {
    var directDeviceDifferences = GetDirectDeviceDifferences( differences );

    if ( !differences.Any() ) {
      log.LogInformation( "No devices found" );
      return;
    }

    foreach ( var diff in directDeviceDifferences ) {
      logger?.LogTrace( "Device diff: {Action} {Path}", diff.DiffType, diff.PropertyPath );
      // Console.WriteLine( $"{diff.DiffType + ":",-10} {diff.PropertyPath}" );

      var state = diff.DiffType;

      var device = state switch {
        DiffType.Unchanged => ( (DiffDevice) diff.Original! ),
        DiffType.Removed => ( (DiffDevice) diff.Original! ),
        DiffType.Added => ( (DiffDevice) diff.Updated! ),
        _ => throw new Exception( "øv" )
      };

      var portDiffs = GetPortDifferences( differences, diff.PropertyPath );

      foreach ( var portDiff in portDiffs ) {
        logger?.LogTrace( "Port diff: {Action} {Path}", portDiff.DiffType, portDiff.PropertyPath );
      }

      var ports = portDiffs.Select( p => {
        return p.DiffType switch {
          DiffType.Unchanged => ( (Port) p.Original! ),
          DiffType.Removed => ( (Port) p.Original! ),
          DiffType.Added => ( (Port) p.Updated! ),
          _ => throw new Exception( "øv" )
        };
      } ).ToList();

      var hostname = device.Get( AddressType.Hostname );
      var mac = device.Get( AddressType.Mac );

      log.Log(
        state == DiffType.Unchanged ? LogLevel.Information : LogLevel.Warning,
        "{State}: IPv4: {Get}, hostname: {Hostname}, MAC: {Mac}, ports: {Ports}",
        state.ToString().ToUpperInvariant(),
        device.Get( AddressType.IpV4 ),
        hostname?.ToLowerInvariant() ?? "",
        mac?.ToUpperInvariant() ?? "",
        string.Join( ",", ports.Select( x => x.Value ) )
      );
    }
  }
}