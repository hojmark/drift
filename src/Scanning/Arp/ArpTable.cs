using System.Collections;
using System.Collections.Frozen;
using System.Net;
using Drift.Domain.Device.Addresses;

namespace Drift.Scanning.Arp;

internal sealed class ArpTable : IEnumerable<KeyValuePair<IPAddress, MacAddress>> {
  internal static readonly ArpTable Empty = new();
  private readonly FrozenDictionary<IPAddress, MacAddress> _map;

  internal ArpTable( Dictionary<IPAddress, MacAddress> map ) {
    _map = map.ToFrozenDictionary();
  }

  private ArpTable() {
    _map = FrozenDictionary.Create<IPAddress, MacAddress>();
  }

  // TODO Replace IPAddress with IPv4 and IPv6
  internal bool TryGetValue( IPAddress ip, out MacAddress mac ) {
    return _map.TryGetValue( ip, out mac );
  }

  public IEnumerator<KeyValuePair<IPAddress, MacAddress>> GetEnumerator() {
    return _map.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return GetEnumerator();
  }
}