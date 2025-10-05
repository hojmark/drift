using System.Reflection;
using Drift.Networking.PeerStreaming.Core.Abstractions;

namespace Drift.Networking.PeerStreaming.Core.Messages;

public interface IPeerMessageTypesProvider {
  IEnumerable<Type> Get();
}

public class AssemblyScanPeerMessageTypesProvider( params Assembly[] assemblies ) : IPeerMessageTypesProvider {
  public IEnumerable<Type> Get() {
    var types = assemblies
      .SelectMany( a => a.GetTypes() )
      .Where( t => typeof(IPeerMessage).IsAssignableFrom( t ) && !t.IsAbstract && !t.IsInterface );

    if ( types.Count() == 0 ) {
      throw new InvalidOperationException(
        $"No types implementing {nameof(IPeerMessage)} found in assemblies: {string.Join( ", ", assemblies.Select( a => a.GetName().Name ) )}" );
    }

    return types;
  }
}