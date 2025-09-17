using Drift.Domain;
using Drift.Domain.NeoProgress;

namespace Drift.Core.Scan;

/*public class ScanProgressBuilder( Action<ProgressNode>? onProgress )
  : ProgressBuilder<ScanProgressRootDefinition>( node => new ScanProgressRootDefinition( node ), onProgress );
*/
public static class ScanProgressFactory {
  public static ScanProgressTree Create( Action<ProgressNode>? onProgress ) {
    var rootNode = new ProgressNode( onProgress ) { Path = "Root" };
    var subnetRoot = rootNode.GetOrCreateChild( ScanPaths.SubnetDiscovery.Self );
    var deviceRoot = rootNode.GetOrCreateChild( ScanPaths.DeviceDiscovery.Self, weight: 99 );

    return new ScanProgressTree
    (
      Root: rootNode,
      SubnetDiscovery: new(
        Parent: rootNode,
        Self: subnetRoot,
        FromInterfaces: subnetRoot.GetOrCreateChild( ScanPaths.SubnetDiscovery.FromInterfaces ),
        FromSpec: subnetRoot.GetOrCreateChild( ScanPaths.SubnetDiscovery.FromSpec )
      ) { Path = ScanPaths.SubnetDiscovery.Self },
      DeviceDiscovery: new(
        Parent: rootNode,
        Self: deviceRoot,
        PingScanning: deviceRoot.GetOrCreateChild( ScanPaths.DeviceDiscovery.PingScanning, weight: 99 ),
        ArpResolution: deviceRoot.GetOrCreateChild( ScanPaths.DeviceDiscovery.ArpResolution )
      ) { Path = ScanPaths.DeviceDiscovery.Self }
    ) { Path = "Root" };
  }
}

public record ScanProgressTree(
  ProgressNode Root,
  SubnetDiscoveryGroup SubnetDiscovery,
  DeviceDiscoveryGroup DeviceDiscovery
) : ProgressNode( Root );

public record SubnetDiscoveryGroup(
  ProgressNode Parent,
  // Action Update, //TODO avoid
  ProgressNode Self,
  ProgressNode /*<InterfaceSubnetDiscoveryData>*/ FromInterfaces,
  ProgressNode FromSpec
) : ProgressNode( Parent );

public record InterfaceSubnetDiscoveryData(
  List<CidrBlock> Subnets //TODO maybe map of subnet -> [source1, source2]
);

public record DeviceDiscoveryGroup(
  ProgressNode Parent,
  //Action Update, //TODO avoid
  ProgressNode Self,
  ProgressNode PingScanning,
  ProgressNode ArpResolution
) : ProgressNode( Parent );