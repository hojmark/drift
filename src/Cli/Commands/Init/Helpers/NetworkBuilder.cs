using System.Text;
using Drift.Domain;
using Drift.Domain.Device.Addresses;
using Drift.Domain.Device.Declared;
using Drift.Spec.Serialization;
using Network = Drift.Domain.Network;

namespace Drift.Cli.Commands.Init.Helpers;

// Network is only part of the spec...
internal class NetworkBuilder {
  private readonly Network _network;
  // private readonly ISerializer _yamlSerializer;

  public NetworkBuilder() {
    _network = new Network();
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
    DeclaredDeviceState? state = DeclaredDeviceState.Up
  ) {
    var device = new DeclaredDevice { Addresses = addresses, Id = id, Enabled = enabled, State = state };

    _network.Devices.Add( device );
    return this;
  }

  public Network Build() {
    // TODO check if valid using JSON schema
    return _network;
  }

  public string ToYaml() {
    var inventory = new Inventory { Network = _network };
    return YamlConverter.Serialize( inventory );
  }

  public void WriteToFile( string specPath ) {
    File.WriteAllText( specPath, ToYaml(), Encoding.UTF8 );
  }
}