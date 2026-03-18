using Drift.Cli.SpecFile;
using Drift.Domain;

namespace Drift.Cli.Tests.SpecFile;

internal sealed class FileSystemSpecProviderTests {
  [TestCase( "/home/user/my-net.spec.yaml", "my-net" )]
  [TestCase( "/home/user/my-net.spec.yml", "my-net" )]
  [TestCase( "/some/deep/path/office.spec.yaml", "office" )]
  [TestCase( @"C:\Users\user\my-net.spec.yaml", "my-net" )]
  [TestCase( @"C:\Users\user\my-net.spec.yml", "my-net" )]
  [TestCase( @"C:\Some\Deep\Path\office.spec.yaml", "office" )]
  public void GetNetworkId_ExtractsNetworkNameFromPath( string path, string expected ) {
    var file = new FileInfo( path );

    var result = FileSystemSpecProvider.GetNetworkId( file );

    Assert.That( result, Is.EqualTo( new NetworkId( expected ) ) );
  }
}