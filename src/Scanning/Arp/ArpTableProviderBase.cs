namespace Drift.Scanning.Arp;

internal abstract class ArpTableProviderBase : IArpTableProvider {
  private readonly Lock _cacheLock = new();
  private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds( 1 );
  private ArpTable _cache = ArpTable.Empty;
  private DateTime _lastUpdated = DateTime.MinValue;

  public ArpTable Cached => GetTable( forceRefresh: false );

  public ArpTable Fresh => GetTable( forceRefresh: true );

  private ArpTable GetTable( bool forceRefresh ) {
    lock ( _cacheLock ) {
      var now = DateTime.UtcNow;

      if ( !forceRefresh && ( now - _lastUpdated ) < _cacheTtl ) {
        return _cache;
      }

      _cache = ReadSystemArpCache();
      _lastUpdated = now;
      return _cache;
    }
  }

  protected abstract ArpTable ReadSystemArpCache();
}