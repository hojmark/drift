using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils.Agent;
using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Cli.Tests.Utils.Network.Topology;
using Drift.Domain;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ScanCommandTests {
  [Test]
  public async Task WithAgents_TwoDisjointSubnets_CombinesResults() {
    // Arrange
    var builder = Topologies.TwoAgentsDisjointSubnetsWithCli()
      .WithFirewall( fw => {
        fw.DefaultPolicy = FirewallAction.Deny;
        // CLI must be able to reach agents to collect their results (control plane = same firewall).
        // Allow CLI→agent routing; deny-default blocks all other cross-subnet traffic (agent↔agent).
        fw.Allow( FirewallTarget.Subnet( "cli-subnet" ), FirewallTarget.Subnet( "agent1-subnet" ) );
        fw.Allow( FirewallTarget.Subnet( "cli-subnet" ), FirewallTarget.Subnet( "agent2-subnet" ) );
      } );
    var topology = builder.Build();
    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.AgentExitCodes, Has.Count.EqualTo( 2 ) );
    Assert.That( result.AgentExitCodes.Values, Is.All.EqualTo( ExitCodes.Success ) );

    // Verify full output
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_TwoDisjointSubnets_CombinesResults)}" );
  }

  [Test]
  public async Task WithAgents_OverlappingSubnets_MergesDevices() {
    // Arrange
    var topology = Topologies.TwoAgentsOverlappingSubnet();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );

    // Verify output shows merged results
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_OverlappingSubnets_MergesDevices)}" );
  }

  [Test]
  public async Task WithAgents_EmptyResults_Succeeds() {
    // Arrange
    var topology = Topologies.SingleAgentEmptyResults();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.AgentExitCodes, Has.Count.EqualTo( 1 ) );
    Assert.That( result.AgentExitCodes.Values, Is.All.EqualTo( ExitCodes.Success ) );

    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_EmptyResults_Succeeds)}" );
  }

  [Test]
  public async Task WithAgents_NoLocalInterfaces_UsesDistributedScanner() {
    // Arrange - Two agents, no CLI config (so CLI has no local interfaces)
    var topology = Topologies.TwoAgentsDisjointSubnetsNoCli();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.AgentExitCodes, Has.Count.EqualTo( 2 ) );
    Assert.That( result.AgentExitCodes.Values, Is.All.EqualTo( ExitCodes.Success ) );

    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_NoLocalInterfaces_UsesDistributedScanner)}" );
  }

  [Test]
  public async Task WithAgents_ScannerSelection_UsesDistributedScanner() {
    // Arrange
    var topology = Topologies.MixedCliAndAgents();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );

    // The output should show both local scan (192.168.0.x) and agent scan (192.168.10.x)
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_ScannerSelection_UsesDistributedScanner)}" );
  }

  [Test]
  public async Task WithAgents_DuplicateDevices_Deduplicates() {
    // Arrange - Both agents discover the same device on the same subnet
    var topology = new NetworkTopologyBuilder()
      .AddSubnet(
        "shared",
        "192.168.10.0/24",
        [
          ( "192.168.10.100", "Shared device" ), // Both agents will see this
          ( "192.168.10.101", "Device from agent1" ),
          ( "192.168.10.102", "Device from agent2" )
        ],
        out var sharedSubnet
      )
      .AddAgent( new AgentId( "agentid_agent1" ), sharedSubnet )
      .AddAgent( new AgentId( "agentid_agent2" ), sharedSubnet )
      .Build();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.AgentExitCodes, Has.Count.EqualTo( 2 ) );
    Assert.That( result.AgentExitCodes.Values, Is.All.EqualTo( ExitCodes.Success ) );

    // The duplicate device (192.168.10.100) should only appear once in merged results
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_DuplicateDevices_Deduplicates)}" );
  }

  [Test]
  public async Task WithAgents_FirewallRules_FiltersVisibility() {
    // Arrange - DMZ agent can see internal, but internal agent cannot see DMZ
    // This tests that firewall rules properly restrict network visibility
    var topology = new NetworkTopologyBuilder()
      .AddSubnet(
        "dmz",
        "192.168.1.0/24",
        [
          ( "192.168.1.100", "Web server" ),
          ( "192.168.1.101", "App server" )
        ],
        out var dmzSubnet
      )
      .AddSubnet(
        "internal",
        "10.0.0.0/24",
        [
          ( "10.0.0.100", "Database" ),
          ( "10.0.0.101", "File server" )
        ],
        out var internalSubnet
      )
      .AddAgent( new AgentId( "agentid_dmz" ), dmzSubnet )
      .AddAgent( new AgentId( "agentid_internal" ), internalSubnet )
      .WithFirewall( fw => {
        // DMZ can see both networks
        fw.Allow( FirewallTarget.Subnet( "dmz" ), FirewallTarget.Subnet( "dmz" ) );
        fw.Allow( FirewallTarget.Subnet( "dmz" ), FirewallTarget.Subnet( "internal" ) );

        // Internal can only see itself
        fw.Allow( FirewallTarget.Subnet( "internal" ), FirewallTarget.Subnet( "internal" ) );
        fw.Deny( FirewallTarget.Subnet( "internal" ), FirewallTarget.Subnet( "dmz" ) ); // Explicit deny
      } )
      .Build();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunScanAsync();

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.AgentExitCodes, Has.Count.EqualTo( 2 ) );
    Assert.That( result.AgentExitCodes.Values, Is.All.EqualTo( ExitCodes.Success ) );

    // Verify output shows DMZ agent scanned both networks, internal agent only scanned internal
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_FirewallRules_FiltersVisibility)}" );
  }
}