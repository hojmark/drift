using Drift.Cli.Abstractions;
using Drift.Cli.Tests.Utils.Agent;
using Drift.Cli.Tests.Utils.Network.Firewall;
using Drift.Cli.Tests.Utils.Network.Topology;

namespace Drift.Cli.Tests.Commands;

internal sealed partial class ScanCommandTests {
  [Test]
  public async Task WithAgents_TwoDisjointSubnets_CombinesResults() {
    // Arrange
    var builder = Topologies.TwoAgentsDisjointSubnetsWithCoordinator()
      .WithFirewall( fw => {
        fw.DefaultPolicy = FirewallAction.Deny;
      } );
    var topology = builder.Build();
    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.EnrolledAgentIds, Has.Count.EqualTo( 2 ) );

    // Verify full output
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_TwoDisjointSubnets_CombinesResults)}" );
  }

  [Test]
  public async Task WithAgents_OverlappingSubnets_UsesSingleAssignment() {
    // Arrange
    var topology = Topologies.TwoAgentsOverlappingSubnet();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );

    // The coordinator assigns an overlapping subnet to one agent.
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_OverlappingSubnets_UsesSingleAssignment)}" );
  }

  [Test]
  public async Task WithAgents_EmptyResults_Succeeds() {
    // Arrange
    var topology = Topologies.SingleAgentEmptyResults();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.EnrolledAgentIds, Has.Count.EqualTo( 1 ) );

    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_EmptyResults_Succeeds)}" );
  }

  [Test]
  public async Task WithAgents_NoLocalInterfaces_UsesRemoteScanning() {
    // Arrange - Two agents, with no coordinator-local interfaces.
    var topology = Topologies.TwoAgentsDisjointSubnetsNoCoordinatorSubnet();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.EnrolledAgentIds, Has.Count.EqualTo( 2 ) );

    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_NoLocalInterfaces_UsesRemoteScanning)}" );
  }

  [Test]
  public async Task WithAgents_ScannerSelection_UsesLocalAndRemoteScanning() {
    // Arrange
    var topology = Topologies.MixedCoordinatorAndAgents();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );

    // The output should show both coordinator-local (192.168.0.x) and remote (192.168.10.x) results.
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_ScannerSelection_UsesLocalAndRemoteScanning)}" );
  }

  [Test]
  public async Task WithAgents_DuplicateDevices_ProducesUniqueResult() {
    // Arrange - Both agents report the same subnet and devices.
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
      .AddAgent( "agent_agent1", sharedSubnet )
      .AddAgent( "agent_agent2", sharedSubnet )
      .Build();

    await using var harness = await AgentTestHarness.CreateAsync( topology );

    // Act
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.EnrolledAgentIds, Has.Count.EqualTo( 2 ) );

    // The result should contain each discovered device once.
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_DuplicateDevices_ProducesUniqueResult)}" );
  }

  [Test]
  public async Task WithAgents_FirewallRules_ControlsAgentVisibility() {
    // Arrange - DMZ agent can see internal, but internal agent cannot see DMZ.
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
      .AddAgent( "agent_dmz", dmzSubnet )
      .AddAgent( "agent_internal", internalSubnet )
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
    var result = await harness.RunCliAsync( "scan" );

    // Assert
    Assert.That( result.ScanExitCode, Is.EqualTo( ExitCodes.Success ) );
    Assert.That( result.EnrolledAgentIds, Has.Count.EqualTo( 2 ) );

    // Verify output contains only devices visible through the agent topology.
    await Verify( result.CombinedOutput )
      .UseFileName( $"{nameof(ScanCommandTests)}.{nameof(WithAgents_FirewallRules_ControlsAgentVisibility)}" );
  }
}