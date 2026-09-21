using System.Text.Json;
using Drift.Spec.Schema;
using Drift.Spec.Serialization;
using Json.Schema;
using YamlDotNet.Core;

namespace Drift.Spec.Validation;

public static class SpecValidator {
  // Keywords that act as containers and report generic "some children failed" messages.
  // Their errors are redundant when child errors are already reported.
  private static readonly HashSet<string> ContainerKeywords = [
    "properties", "patternProperties", "additionalProperties",
    "items", "prefixItems", "unevaluatedItems", "unevaluatedProperties",
    "allOf", "anyOf", "oneOf", "not", "if", "then", "else",
    "dependentSchemas", "propertyNames", "propertyDependencies",
  ];

  public static ValidationResult Validate( string yaml, SpecVersion version ) {
    try {
      var schema = SpecSchemaProvider.Get( version );
      return Validate( yaml, schema );
    }
    // TODO add failed parsing tests
    // An (unexpected) parsing error
    catch ( YamlException ex ) {
      var errors = new List<ValidationError>();

      Exception? exp = ex;
      do {
        errors.Add( new ValidationError { Message = exp.Message } );
        exp = exp.InnerException;
      } while ( exp != null );

      return new ValidationResult { IsValid = false, Errors = errors };
    }
  }

  private static ValidationResult Validate( string yaml, JsonSchema schema ) {
    // Read YAML and convert to JSON
    var yamlObject = YamlConverter.DeserializeToDto( yaml );
    // var yamlLineNumbers = GetYamlLineNumbers( yamlContent );
    var jsonString = YamlConverter.SerializeToDto( yamlObject, true );
    var jsonDocument = JsonDocument.Parse( jsonString );

    // Validate
    var validationResults = schema.Evaluate(
      jsonDocument.RootElement,
      new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical }
    );

    return new ValidationResult {
      IsValid = validationResults.IsValid, Errors = ExtractErrors( validationResults ).ToList()
    };
  }

  private static IEnumerable<ValidationError> ExtractErrors( EvaluationResults results ) {
    if ( results.IsValid ) {
      yield break;
    }

    if ( results.Errors?.Count > 0 ) {
      foreach ( var error in results.Errors.Where( e => !ContainerKeywords.Contains( e.Key ) ) ) {
        yield return new ValidationError {
          Path = results.InstanceLocation.SegmentCount == 0 ? "/" : results.InstanceLocation.ToString(),
          Message = error.Value,
          SchemaPath = results.SchemaLocation.ToString()
        };
      }
    }

    if ( results.Details != null ) {
      foreach ( var detail in results.Details ) {
        foreach ( var nestedError in ExtractErrors( detail ) ) {
          yield return nestedError;
        }
      }
    }
  }
}