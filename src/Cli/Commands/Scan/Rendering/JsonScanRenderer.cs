using System.Text.Json;
using System.Text.Json.Serialization;
using Drift.Cli.Commands.Scan.Models;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Spectre.Console;

namespace Drift.Cli.Commands.Scan.Rendering;

internal sealed class JsonScanRenderer( IJsonOutput output ) : IRenderer<List<Subnet>> {
  public void Render( List<Subnet> subnets ) {
    var result = new JsonScanOutput(
      subnets.Select( subnet => new JsonSubnet(
          subnet.Cidr.ToString(),
          subnet.Devices.Count,
          subnet.Devices.Select( device => new JsonDevice(
              device.Ip.WithoutMarkup,
              device.Mac.WithoutMarkup,
              Markup.Remove( device.State.Text )
            )
          ).ToArray()
        )
      ).ToArray()
    );

    output.WriteLine( JsonSerializer.Serialize( result, JsonScanJsonSerializerContext.Default.JsonScanOutput ) );
  }
}

internal sealed record JsonScanOutput( JsonSubnet[] Subnets );

internal sealed record JsonSubnet( string Cidr, int DeviceCount, JsonDevice[] Devices );

internal sealed record JsonDevice( string Ip, string Mac, string State );

[JsonSourceGenerationOptions( PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true )]
[JsonSerializable( typeof(JsonScanOutput) )]
internal sealed partial class JsonScanJsonSerializerContext : JsonSerializerContext;