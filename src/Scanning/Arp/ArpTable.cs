using System.Net;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

internal sealed class ArpTable {
  internal static readonly ArpTable Empty = new();
  private readonly Dictionary<IPAddress, MacAddress> _map;

  internal ArpTable( Dictionary<IPAddress, MacAddress> map ) {
    _map = map;
  }

  private ArpTable() {
    _map = new();
  }

  // TODO Replace IPAddress with IPv4 and IPv6
  internal bool TryGetValue( IPAddress ip, out MacAddress mac ) {
    return _map.TryGetValue( ip, out mac );
  }
}