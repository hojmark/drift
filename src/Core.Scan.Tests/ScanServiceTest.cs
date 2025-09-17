using System.Net;
using System.Text.RegularExpressions;
using Drift.Core.Scan.Model;
using Drift.Core.Scan.Subnet;
using Drift.Domain;
using Drift.Domain.NeoProgress;
using Drift.TestUtilities;
using Drift.Utils.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Core.Scan.Tests;

public class ScanServiceTest {
  [Test]
  [TestCase( "192.168.0.0/24" )]
  public async Task Test( params string[] cidrs ) {
    // Arrange
    var subnets = cidrs.Select( cidr => new CidrBlock( cidr ) ).ToList();
    var successfulIps = subnets.SelectMany( cidr => IPNetwork2
      .Parse( cidr.ToString() )
      .ListIPAddress( Filter.Usable )
      .Select( ip => IPAddress.Parse( ip.ToString() ) )
      .Take( 3 )
    ).ToList();
    var pingTool = new TestPingTool( successfulIps );

    // Arrange
    var interfaceSubnetProvider = new PhysicalInterfaceSubnetProvider( NullLogger.Instance );
    //var pingTool = new OsPingTool()
    var service = new ScanService( interfaceSubnetProvider, pingTool );
    var scanRequest = new ScanRequest();
    var logger = new StringLogger();
    Action<ProgressNode>? onProgress = progressReport => {
      logger.LogInformation(
        "{Phase}: {StatusMessage} ({CompletionPct})",
        //progressReport.CurrentPhase,
        //progressReport.StatusMessage,
        null,
        null,
        progressReport.TotalProgress
      );
    };

    // Act
    var result = await service.ScanAsync( scanRequest, onProgress );

    // Assert
    //await Verify( logger.ToString() );
    Assert.That( result, Is.Not.Null );
    Assert.That( result.Result.Status, Is.EqualTo( ScanResultStatus.Success ) );
    await Verify( logger.ToString() )
      .ScrubLinesWithReplace( line =>
        Regex.Replace(
          Regex.Replace(
            line,
            @"\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}",
            "<time>"
          ),
          "in .+",
          "in <elapsed>"
        )
      );
  }
}