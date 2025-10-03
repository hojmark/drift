using System.Text.RegularExpressions;
using Drift.Cli.Commands.Scan.Interactive;
using Drift.Cli.Commands.Scan.Models;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.Presentation.Rendering.DeviceState;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using NaturalSort.Extension;

namespace Drift.Cli.Commands.Scan.ResultProcessors;

internal static class SubnetScanResultProcessor {
  private const string Na = "n/a";
  private static int _deviceIdCounter = 0;

  internal static List<Device> Process( SubnetScanResult scanResult, Network? network ) {
    // TODO test throw new Exception( "ads" );

    var declaredDevices = network == null ? [] : network.Devices.Where( d => d.IsEnabled() ).ToList();

    var discoveredDevices = scanResult.DiscoveredDevices.ToList();

    var directDiffs = GetDirectDeviceDifferences( declaredDevices, discoveredDevices );

    var result = new List<Device>();

    foreach ( var diff in directDiffs ) {
      // logger?.LogTrace( "Device diff: {Action} {Path}", diff.DiffType, diff.PropertyPath );
      // Console.WriteLine( $"{diff.DiffType + ":",-10} {diff.PropertyPath}" );

      var device = GetDevice( diff );

      var matchingDeclared = declaredDevices
        .Where( d => ( (IAddressableDevice) d ).GetDeviceId() == device.GetDeviceId() )
        .ToList();

      if ( matchingDeclared.Count > 1 ) {
        // TODO review error message
        throw new Exception(
          "Multiple declared devices have the same ID: " +
          string.Join( ", ", matchingDeclared.Select( d => d.Id ) )
        );
      }

      var declaredDevice = matchingDeclared.SingleOrDefault();
      var displayDevice = CreateDisplayDevice( diff.DiffType, device, declaredDevice, scanResult );

      result.Add( displayDevice );
    }

    // Order by IP
    return result
      .OrderBy( dev => dev.Ip.WithoutMarkup, StringComparer.OrdinalIgnoreCase.WithNaturalSort() )
      .ToList();
  }

  private static IAddressableDevice GetDevice( ObjectDiff diff ) {
    return diff.DiffType switch {
      // Note: may be unchanged based on device id, but other value may be updated in which case we'd like to show the updated values... but this is debatable... maybe both or a merge should be shown
      DiffType.Unchanged => (DiffDevice) diff.Updated!,
      DiffType.Removed => (DiffDevice) diff.Original!,
      DiffType.Added => (DiffDevice) diff.Updated!,
      _ => throw new Exception( $"Unexpected {nameof(DiffType)}: {diff.DiffType}" )
    };
  }

  private static Device CreateDisplayDevice(
    DiffType state,
    IAddressableDevice device,
    DeclaredDevice? declaredDevice,
    SubnetScanResult scanResult
  ) {
    var declaredState = declaredDevice?.State;
    var discoveredState = state == DiffType.Removed ? DiscoveredDeviceState.Offline : DiscoveredDeviceState.Online;

    const bool unknownAllowed = true; // TODO should come from scan options

    var ip = device.Get( AddressType.IpV4 );
    var mac = device.Get( AddressType.Mac );

    var deviceRenderState = DeviceRenderState.From( declaredState, discoveredState, unknownAllowed );
    deviceRenderState = ip == null || scanResult.DiscoveryAttempts.Contains( new IpV4Address( ip ) )
      ? deviceRenderState
      : new DeviceRenderState( deviceRenderState.State, deviceRenderState.Icon, "[grey bold]Unknown[/]" );

    var id = InteractiveUi.FakeData ? GenerateDeviceId() : declaredDevice?.Id;
    var displayId = id == null ? "[grey][/]" : $"[cyan]{id}[/]";

    var declaredDeviceId = ( declaredDevice as IAddressableDevice )?.GetDeviceId();

    return new Device {
      State = deviceRenderState,
      Ip = new DisplayValue( DeviceIdHighlighter.Mark( ip ?? Na, AddressType.IpV4, declaredDeviceId ) ),
      Mac = new DisplayValue(
        DeviceIdHighlighter.Mark(
          mac != null ? ( InteractiveUi.FakeData ? GenerateMacAddress() : mac ) : Na,
          AddressType.Mac,
          declaredDeviceId
        )
      ),
      Id = new DisplayValue( displayId ),
    };
  }

  private static List<ObjectDiff> GetDirectDeviceDifferences(
    IReadOnlyList<DeclaredDevice> declared,
    IReadOnlyList<DiscoveredDevice> discovered
  ) {
    var differences = ObjectDiffEngine.Compare(
      declared.ToDiffDevices(),
      discovered.ToDiffDevices(),
      "Device",
      new DiffOptions()
        .ConfigureDiffDeviceKeySelectors( declared )
        // Includes Unchanged, which makes for an easier table population
        .SetDiffTypesAll()
    );

    return differences
      .Where( d => Regex.IsMatch( d.PropertyPath, @"^Device\[[^\]]+?\]$" ) )
      .OrderBy( d => d.PropertyPath, StringComparison.OrdinalIgnoreCase.WithNaturalSort() )
      .ToList();
  }

  private static string GenerateMacAddress() {
    var rand = new Random();
    byte[] macBytes = new byte[6];
    rand.NextBytes( macBytes );
    return string.Join( "-", macBytes.Select( b => b.ToString( "X2" ) ) );
  }

  private static string GenerateDeviceId() {
    _deviceIdCounter++;
    return "device-" + _deviceIdCounter;
  }
}