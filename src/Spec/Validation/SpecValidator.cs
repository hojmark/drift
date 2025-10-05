using System.Text.Json;
using Drift.Spec.Schema;
using Drift.Spec.Serialization;
using Json.Schema;
using YamlDotNet.Core;
using SpecVersion = Drift.Spec.Schema.SpecVersion;

namespace Drift.Spec.Validation;

public static class SpecValidator {
  public static ValidationResult Validate( string yaml, SpecVersion specVersion ) {
    try {
      var schema = SpecSchemaProvider.AsText( specVersion );
      return Validate( yaml, schema );
    }
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

  private static ValidationResult Validate( string yaml, string jsonSchema ) {
    // try {
    // Read YAML and convert to JSON
    var yamlObject = YamlConverter.DeserializeToDto( yaml );
    // var yamlLineNumbers = GetYamlLineNumbers( yamlContent );
    var jsonString = YamlConverter.SerializeToDto( yamlObject, true );
    var jsonDocument = JsonDocument.Parse( jsonString );

    // Read JSON schema
    var schema = JsonSchema.FromText( jsonSchema );

    // Validate
    var validationResults = schema.Evaluate(
      jsonDocument.RootElement,
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
    if ( results.IsValid ) {
      yield break;
    }

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