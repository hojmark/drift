using Drift.Spec.Schema;
using Drift.Spec.Validation;
using Drift.TestUtilities;

namespace Drift.Spec.Tests;

internal sealed class ValidationTests {
  [TestCase(
    1,
    """

    """,
    "/: Required properties [\"version\",\"network\"] are not present"
  )]
  [TestCase(
    2,
    """
    # Empty
    """,
    "/: Required properties [\"version\",\"network\"] are not present"
  )]
  [TestCase(
    3,
    """
    settings:
    """,
    "/: Required properties [\"version\",\"network\"] are not present"
  )]
  [TestCase(
    4,
    """
    version: v1-preview
    """,
    "/: Required properties [\"network\"] are not present"
  )]
  [TestCase(
    5,
    """
    version: invalid-version
    """,
    "/: Required properties [\"network\"] are not present",
    "/version: Expected \"\\u0022v1-preview\\u0022\""
  )]
  // Subnets not required
  /*[TestCase( 4,
    """
    version: v1-preview
    network:
      id: my-network
    """,
    "/network: Required properties [\"subnets\"] are not present" )]
  [TestCase( 5,
    """
    version: v1-preview
    network:
      subnets:
    """,
    "/network: Required properties [\"subnets\"] are not present" )]
  [TestCase( 6,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
    """,
    "/network: Required properties [\"subnets\"] are not present" )]*/
  [TestCase(
    6,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
    """,
    "/network/subnets/0: Required properties [\"address\"] are not present"
  )]
  [TestCase(
    7,
    """
    version: invalid-version
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    """,
    "/version: Expected \"\\u0022v1-preview\\u0022\""
  )]
  [TestCase(
    8,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    settings:
      unknown_devices: not-a-valid-value
    """,
    ": Exception during deserialization",
    ": Requested value 'NotAValidValue' was not found."
  )]
  [TestCase(
    9,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    server:
      foo: bar
    """,
    "/server: Required properties [\"address\"] are not present"
  )]
  [TestCase(
    10,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    agents:
      - id: agentid_test1
        address: http://agent1:5000
        policy:
          - expect: reachable
    """,
    "/agents/0/policy/0: Required properties [\"to\"] are not present"
  )]
  [TestCase(
    11,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
      devices:
        - id: my-device
    """,
    "/network/devices/0: Required properties [\"addresses\"] are not present"
  )]
  public void YamlIsInvalidTest( int caseNo, string yaml, params string[] errors ) {
    // Arrange / Act
    var result = SpecValidator.Validate( yaml, SpecVersion.V1_preview );
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.IsValid, Is.False, "Expected YAML to be invalid, but it was not" );
      Assert.That( result.Errors, Is.Not.Empty );
      Assert.That( result.Errors.Select( e => e.ToString() ), Is.EquivalentTo( errors ) );
    }

    Console.WriteLine( "Validation errors:" );
    Console.WriteLine( result.ToString() );
  }

  [TestCase(
    1,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 192.168.0.100/24
    """ )]
  [TestCase(
    2,
    """
    version: "v1-preview"
    network:
      id: "my-network"
      subnets:
        - id: "my-subnet"
          address: "100.100.100.100/16"
    """ )]
  [TestCase(
    3,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    """ )]
  [TestCase(
    4,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    settings:
      unknown_devices: disallowed
      undeclared_connections: blocked
      ping_throttling: 100
      scan_only_declared_subnets: true
    """ )]
  [TestCase(
    5,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    server:
      address: http://192.168.1.10:5000
    """ )]
  [TestCase(
    6,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    agents:
      - id: agentid_test1
        address: http://agent1:5000
        policy:
          - to: [internet, servers]
            expect: reachable
          - to: [router]
            port: [443]
            protocol: tcp
            probe: [tls]
            expect: reachable
            fallback: gateway
    """ )]
  [TestCase(
    7,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
      devices:
        - id: nas
          addresses:
            ipv4: 192.168.10.5
            mac: b8:27:eb:12:34:56
          info:
            hostname: nas.local
    """ )]
  public void YamlIsValidTest( int caseNo, string yaml ) {
    // Arrange / Act
    var result = SpecValidator.Validate( yaml, SpecVersion.V1_preview );
    using ( Assert.EnterMultipleScope() ) {
      Assert.That( result.IsValid, Is.True, result.ToUnitTestMessage() );
      Assert.That( result.Errors, Is.Empty );
    }
  }
}