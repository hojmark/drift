using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace Drift.Common.Schemas;

public class LowerCaseEnumGenerator : ISchemaGenerator {
  public bool Handles( Type type ) => type.IsEnum;

  public void AddConstraints( SchemaGenerationContextBase context ) {
    var enumNames = Enum.GetNames( context.Type )
      .Select( name => name.ToLowerInvariant() )
      .ToArray();

    context.Intents.Add( new EnumIntent( enumNames ) );
  }
}