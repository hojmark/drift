using Drift.Cli.Commands.Scan.Models;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.Presentation.Rendering.DeviceState;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Scan.Rendering;

internal class LogScanRenderer( ILogOutput log ) : IRenderer<List<Subnet>> {
  public void Render( List<Subnet> subnets ) {
    foreach ( var subnet in subnets ) {
      var conformant = subnet.Devices.All( d => d.State.State.IsConformant() );

      log.Log(
        conformant ? LogLevel.Information : LogLevel.Warning,
        "Subnet {Cidr} conforms to spec: {Conformant}",
        subnet.Cidr,
        conformant
      );

      foreach ( var device in subnet.Devices ) {
        log.LogWarning( "Device" );
        log.Log(
          device.State.State.IsConformant() ? LogLevel.Information : LogLevel.Warning,
          "IPv4: {Get}, MAC: {Mac}, Conformant: {Conformant}, State: {State}",
          device.Ip.WithoutMarkup,
          device.Mac.WithoutMarkup,
          device.State.State.IsConformant(),
          device.State.State
        );
      }
    }
  }
}