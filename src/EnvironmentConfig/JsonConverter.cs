using System.Text.Json;
using System.Text.Json.Serialization;

namespace Drift.EnvironmentConfig;

//TODO abused elsewhere. rename to EnvironmentJsonConverter to test
public static class JsonConverter {
  private static readonly JsonSerializerOptions SerializerOptions = new() {
    ReadCommentHandling = JsonCommentHandling.Skip, // or skip if not using the comment
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Converters = { new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ) }
  };

  public static T Deserialize<T>( string json ) {
    throw new NotImplementedException();
    //return JsonSerializer.Deserialize<T>( json, SerializerOptions );
  }

  public static T Deserialize<T>( Stream stream ) {
    throw new NotImplementedException();
    //return JsonSerializer.Deserialize<T>( stream, SerializerOptions );
  }

  public static string Serialize( object environment ) {
    return JsonSerializer.Serialize( environment, SerializerOptions );
  }
}