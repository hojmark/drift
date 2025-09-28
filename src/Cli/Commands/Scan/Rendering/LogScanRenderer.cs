using Drift.Cli.Output.Abstractions;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Extensions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class LogScanRenderer( ILogOutput log ) : DiffRendererBase {
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

      var hostname = device.Get( AddressType.Hostname );
      var mac = device.Get( AddressType.Mac );

      log.Log(
        state == DiffType.Unchanged ? LogLevel.Information : LogLevel.Warning,
        "{State}: IPv4: {Get}, hostname: {Hostname}, MAC: {Mac}",
        state.ToString().ToUpperInvariant(),
        device.Get( AddressType.IpV4 ),
        hostname?.ToLowerInvariant() ?? "",
        mac?.ToUpperInvariant() ?? ""
      );
    }
  }
}