using Drift.Cli.Commands.Scan.Models;

namespace Drift.Cli.Commands.Scan.Interactive.Ui;

internal static class SubnetRenderingExtensions {
  internal static int GetHeight( this List<Subnet> subnets ) => subnets.Sum( GetHeight );

  internal static int GetHeight( this Subnet subnet ) => 1 + ( subnet.IsExpanded ? subnet.Devices.Count : 0 );

  internal static int GetIpWidth( this List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Ip.WithoutMarkup.Length );
  }

  internal static int GetMacWidth( this List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Mac.WithoutMarkup.Length );
  }

  internal static int GetIdWidth( this List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.Id.WithoutMarkup.Length );
  }

  internal static int GetStateTextWidth( this List<Subnet> subnets ) {
    // TODO raw version does not exist
    return subnets.SelectMany( s => s.Devices ).Max( d => d.State.Text.Length );
  }
}