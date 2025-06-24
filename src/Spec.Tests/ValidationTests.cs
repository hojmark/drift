using Drift.Spec.Validation;
using Drift.TestUtilities;

namespace Drift.Spec.Tests;

[TestFixture]
public class ValidationTests {
  [TestCase( 1,
    """

    """,
    //TODO not a great message
    "/: Value is \"null\" but should be \"object\"" )]
  // Network not required
  /*[TestCase( 2,
   """
                version: v1-preview
                """, "Almost empty YAML" )]*/
  // Network not required and version not enforced
  /*[TestCase( 3,
   """
                version: invalid-version
                """, "Invalid version" )]*/
  [TestCase( 4,
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
    "/network: Required properties [\"subnets\"] are not present" )]
  [TestCase( 7,
    """
    version: invalid-version
    network:
      id: my-network
      subnets:
        - id: my-subnet
    """,
    "/network/subnets/0: Required properties [\"address\"] are not present" )]
  // Version not enforced
  /*[TestCase( 8, """
                version: invalid-version
                network:
                  id: my-network
                  subnets:
                    - id: my-subnet
                      address: 100.100.100.100/16
                """, "Invalid version" )]*/
  public void YamlIsInvalidTest( int caseNo, string yaml, params string[] errors ) {
    // Arrange / Act
    var result = YamlValidator.Validate( yaml, Spec.Schema.DriftSpecVersion.V1_preview );

    // Assert
    Assert.Multiple( () => {
      Assert.That( result.IsValid, Is.False, "Expected YAML to be invalid, but it was not" );
      Assert.That( result.Errors, Is.Not.Empty );
      Assert.That( result.Errors.Select( e => e.ToString() ), Is.EquivalentTo( errors ) );
    } );

    Console.WriteLine( "Validation errors:" );
    Console.WriteLine( result.ToString() );
  }

  [TestCase( 1,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 192.168.0.100/24
    """ )]
  [TestCase( 2,
    """
    version: "v1-preview"
    network:
      id: "my-network"
      subnets:
        - id: "my-subnet"
          address: "100.100.100.100/16"
    """ )]
  [TestCase( 3,
    """
    version: v1-preview
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    """ )]
  // TODO version should be required
  [TestCase( 4,
    """
    network:
      id: my-network
      subnets:
        - id: my-subnet
          address: 100.100.100.100/16
    """ )]
  public void YamlIsValidTest( int caseNo, string yaml ) {
    // Arrange / Act
    var result = YamlValidator.Validate( yaml, Spec.Schema.DriftSpecVersion.V1_preview );

    // Assert
    Assert.Multiple( () => {
      Assert.That( result.IsValid, Is.True, result.ToUnitTestMessage() );
      Assert.That( result.Errors, Is.Empty );
    } );
  }
}