using Drift.Cli.Commands.Scan.Interactive.Models;

namespace Drift.Cli.Commands.Scan.Interactive;

internal static class SubnetRenderingExtensions {
  internal static int GetHeight( this List<Subnet> subnets ) => subnets.Sum( GetHeight );

  internal static int GetHeight( this Subnet subnet ) => 1 + ( subnet.IsExpanded ? subnet.Devices.Count : 0 );

  internal static int GetIpWidth( this List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.IpRaw.Length );
  }

  internal static int GetMacWidth( this List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.MacRaw.Length );
  }

  internal static int GetIdWidth( this List<Subnet> subnets ) {
    return subnets.SelectMany( s => s.Devices ).Max( d => d.IdRaw.Length );
  }
  internal static int GetStateTextWidth( this List<Subnet> subnets ) {
    //TODO raw version does not exist
    return subnets.SelectMany( s => s.Devices ).Max( d => d.StateText.Length );
  }
}