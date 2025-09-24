namespace Drift.Scanning.Arp;

internal interface IArpTableProvider {
  internal ArpTable Cached {
    get;
  }

  internal ArpTable Fresh {
    get;
  }
}