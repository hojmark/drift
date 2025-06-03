using YamlDotNet.Serialization;

namespace Drift.Domain;

[YamlSerializable]
// TODO The valid range for TCP/UDP port numbers is 0 to 65535. Include 0 or start from 1? 0 indicates a free ephemeral port.
public /*readonly*/ record struct Port( int Value ) {
  public static implicit operator Port( int value ) => new(value);
  public static implicit operator int( Port port ) => port.Value;
}