using System.Text.Json;
using Drift.Spec.Schema;
using Drift.Spec.Serialization;
using Json.Schema;

namespace Drift.Spec.Validation;

public class SpecValidator {
  public static ValidationResult Validate( string yaml, Schema.SpecVersion specVersion ) {
    var schema = SpecSchemaProvider.AsText( specVersion );
    return Validate( yaml, schema );
  }

  private static ValidationResult Validate( string yaml, string jsonSchema ) {
    //try {
    // Read YAML and convert to JSON
    var yamlObject = YamlConverter.DeserializeToDto( yaml );
    //var yamlLineNumbers = GetYamlLineNumbers( yamlContent );
    var jsonString = YamlConverter.SerializeToDto( yamlObject, true );
    var jsonDocument = JsonDocument.Parse( jsonString );

    // Read JSON schema
    var schema = JsonSchema.FromText( jsonSchema );

    // Validate
    var validationResults = schema.Evaluate( jsonDocument.RootElement,
      new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical }
    );

    var r = new ValidationResult {
      IsValid = validationResults.IsValid, Errors = ExtractErrors( validationResults ).ToList()
    };
    return r;

    // Throw exceptions
    /*}
    catch ( Exception ex ) {
      return new ValidationResult {
        IsValid = false,
        Errors = [
          new ValidationError { Message = $"Validation failed: {ex.Message}", Path = "/" }
        ]
      };
    }*/
  }

  private static IEnumerable<ValidationError> ExtractErrors( EvaluationResults results ) {
    if ( results.IsValid ) yield break;

    if ( results.Errors?.Count > 0 ) {
      foreach ( var error in results.Errors ) {
        yield return new ValidationError {
          Path = results.InstanceLocation.Count == 0 ? "/" : results.InstanceLocation.ToString(),
          Message = error.Value ?? "Validation error",
          SchemaPath = results.SchemaLocation.ToString()
        };
      }
    }

    foreach ( var detail in results.Details ) {
      foreach ( var nestedError in ExtractErrors( detail ) ) {
        yield return nestedError;
      }
    }
  }
}