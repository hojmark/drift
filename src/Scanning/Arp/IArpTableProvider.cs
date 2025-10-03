namespace Drift.Scanning.Arp;

internal interface IArpTableProvider {
  /// <summary>
  /// Gets an application cached ARP table.
  /// </summary>
  internal ArpTable Cached {
    get;
  }

  /// <summary>
  /// Gets a fresh ARP table from the OS. The OS may be cache it.
  /// </summary>
  internal ArpTable Fresh {
    get;
  }
}