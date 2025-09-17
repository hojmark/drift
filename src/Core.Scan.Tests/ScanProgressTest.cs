using Drift.Domain.NeoProgress;
using Drift.TestUtilities;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Tests;

public class ScanProgressTest {
  [Test]
  public async Task FlowNew() {
    var logger = new StringLogger();

    Action<ProgressNode> onProgress = a => logger.LogInformation( a.TotalProgress + "%: {Path}", a.Path );

    var root = ScanProgressFactory.Create( onProgress );

    Assert.That( root.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromInterfaces.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromSpec.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.PingScanning.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.ArpResolution.TotalProgress, Is.EqualTo( 0 ) );

    root.SubnetDiscovery.FromInterfaces.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromInterfaces.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromSpec.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.PingScanning.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.ArpResolution.TotalProgress, Is.EqualTo( 0 ) );

    root.SubnetDiscovery.FromSpec.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromInterfaces.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromSpec.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.PingScanning.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.ArpResolution.TotalProgress, Is.EqualTo( 0 ) );

    root.DeviceDiscovery.PingScanning.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromInterfaces.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromSpec.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.PingScanning.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.ArpResolution.TotalProgress, Is.EqualTo( 0 ) );

    root.DeviceDiscovery.ArpResolution.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromInterfaces.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.SubnetDiscovery.FromSpec.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.PingScanning.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( root.DeviceDiscovery.ArpResolution.TotalProgress, Is.EqualTo( 0 ) );

    await Verify( logger.ToString() );
  }
}