using System.Text.RegularExpressions;
using Drift.Cli.Commands.Scan.Interactive.Models;
using Drift.Diff;
using Drift.Diff.Domain;
using Drift.Domain;
using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Discovered;
using Drift.Domain.Extensions;
using Drift.Domain.Scan;
using NaturalSort.Extension;

namespace Drift.Cli.Commands.Scan.Interactive.ScanResultProcessors;

internal static class SubnetScanResultProcessor {
  internal static List<Device> Process( SubnetScanResult scanResult, Network? network ) {
    //TODO test throw new Exception( "ads" );
    var original = network == null ? [] : network.Devices.Where( d => d.IsEnabled() ).ToList();
    var declaredDevices = original;
    var updated1 = scanResult.DiscoveredDevices;

    var differences = ObjectDiffEngine.Compare(
      original.ToDiffDevices(),
      updated1.ToDiffDevices(),
      "Device",
      new DiffOptions()
        .ConfigureDiffDeviceKeySelectors( original.ToList() )
        // Includes Unchanged, which makes for an easier table population
        .SetDiffTypesAll()
      //, logger //TODO support ioutputmanager or create ilogger adapter?
    );

    var directDeviceDifferences = GetDirectDeviceDifferences( differences );

    var devices = new List<Device>();

    foreach ( var diff in directDeviceDifferences ) {
      //logger?.LogTrace( "Device diff: {Action} {Path}", diff.DiffType, diff.PropertyPath );
      // Console.WriteLine( $"{diff.DiffType + ":",-10} {diff.PropertyPath}" );

      var state = diff.DiffType;

      IAddressableDevice device = state switch {
        // Note: may be unchanged based on device id, but other value may be updated in which case we'd like to show the updated values... but this is debatable... maybe both or a merge should be shown
        DiffType.Unchanged => ( (DiffDevice) diff.Updated! ),
        DiffType.Removed => ( (DiffDevice) diff.Original! ),
        DiffType.Added => ( (DiffDevice) diff.Updated! ),
        _ => throw new Exception( "øv" )
      };

      /*
       * TODO target status:
       *
       * Icon:
       * - Closed circle: online
       * - Open circle: offline
       * - Question mark: unknown device (not in spec)
       * - Exclamation mark: unknown device (not in spec) that has been disallowed by general setting (unknown devices not allowed)
       *
       * Color:
       * - Green: expected state
       * - Red: opposite of expected state
       * - Yellow: undefined state (Q: both because the device is unknown AND because the state of a known device hasn't been specified)
       * Note: could have option for treating unknown devices as disallowed, thus red instead of default yellow
       *
       */

      var declaredDeviceMultiple = declaredDevices.Where( d =>
        ( (IAddressableDevice) d ).GetDeviceId() == device.GetDeviceId()
      ).ToList();

      if ( declaredDeviceMultiple.Count > 1 ) {
        // TODO review error message
        throw new Exception(
          "Multiple declared devices have the same ID: " +
          string.Join( ", ", declaredDeviceMultiple.Select( d => d.Id ) )
        );
      }

      var declaredDevice = declaredDeviceMultiple.SingleOrDefault();
      var declaredDeviceState = declaredDevice?.State;
      var discoveredDeviceState = state == DiffType.Removed
        ? DiscoveredDeviceState.Offline
        : DiscoveredDeviceState.Online;

      const bool unknownAllowed = true;

      var ip = device.Get( AddressType.IpV4 );

      var textStatus = ip == null || scanResult.DiscoveryAttempts.Contains( new IpV4Address( ip ) )
        ? DeviceStateIndicator.GetText(
          declaredDeviceState,
          discoveredDeviceState,
          state == DiffType.Added,
          unknownAllowed,
          onlyDrifted: false
        )
        : "[grey bold]Unknown[/]";

      var mac = device.Get( AddressType.Mac );
      var id = InteractiveUi.FakeData ? GenerateDeviceId() : declaredDevice?.Id;
      id = id == null ? "[grey][/]" : ( $"[cyan]{id}[/]" );

      var deviceId = device.GetDeviceId();
      var deviceIdDeclared = ( declaredDevice as IAddressableDevice )?.GetDeviceId();

      var na = "n/a";

      var d = new Device {
        State = DeviceStateIndicator.GetIcon(
          declaredDeviceState,
          discoveredDeviceState,
          state == DiffType.Added,
          unknownAllowed
        ),
        StateText = textStatus,
        Ip = new DisplayValue( DeviceIdHighlighter.Mark( ip ?? na, AddressType.IpV4, deviceIdDeclared ) ),
        Mac = new DisplayValue(
          DeviceIdHighlighter.Mark(
            mac != null ? ( InteractiveUi.FakeData ? GenerateMacAddress() : mac ) : na,
            AddressType.Mac,
            deviceIdDeclared
          )
        ),
        Id = new DisplayValue( id ),
      };

      devices.Add( d );
    }

    // Order by IP
    devices = devices.OrderBy( dev => dev.Ip.WithoutMarkup, StringComparer.OrdinalIgnoreCase.WithNaturalSort() )
      .ToList();

    return devices;
  }

  private static List<ObjectDiff> GetDirectDeviceDifferences( List<ObjectDiff> differences ) {
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

  private static int _deviceIdCounter = 0;

  private static string GenerateDeviceId() {
    _deviceIdCounter++;
    return "device-" + _deviceIdCounter;
  }
}