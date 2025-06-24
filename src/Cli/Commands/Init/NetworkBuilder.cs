using System.Text;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Spec.Serialization;

namespace Drift.Cli.Commands.Init;

// Network is only part of the spec...
public class NetworkBuilder {
  private readonly Network _network;
  //private readonly ISerializer _yamlSerializer;

  public NetworkBuilder( string id ) {
    _network = new Network { Id = id };
    /*_yamlSerializer = new SerializerBuilder()
      .WithNamingConvention( CamelCaseNamingConvention.Instance )
      .Build();*/
  }

  public NetworkBuilder AddSubnet( CidrBlock address, string? id = null, bool? enabled = null ) {
    var subnet = new DeclaredSubnet { Id = id, Address = address.ToString(), Enabled = enabled };
    _network.Subnets.Add( subnet );
    return this;
  }

  public NetworkBuilder AddDevice(
    List<IDeviceAddress> addresses,
    string? id = null,
    bool? enabled = null,
    DeclaredDeviceState? state = null
  ) {
    var device = new DeclaredDevice { Addresses = addresses, Id = id, Enabled = enabled, State = state };

    _network.Devices.Add( device );
    return this;
  }

  /*public string ToYaml() {
    return _yamlSerializer.Serialize( _network );
  }*/

  public Network Build() {
    //TODO check if valid using JSON schema
    return _network;
  }

  /*public void SaveToFile( string filePath ) {
    var yaml = ToYaml();
    File.WriteAllText( filePath, yaml );
  }*/
  public void WriteYaml( string specPath ) {
    var inventory = new Inventory { Network = _network };

    var yamlContents = YamlConverter.Serialize( inventory );
    File.WriteAllText( specPath, yamlContents, Encoding.UTF8 );
  }
}