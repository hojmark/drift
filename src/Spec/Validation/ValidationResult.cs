namespace Drift.Spec.Validation;

public class ValidationResult {
  public bool IsValid {
    get;
    init;
  }

  public List<ValidationError> Errors {
    get;
    init;
  } = [];

  public override string ToString() {
    var errors = Errors.Count == 0
      ? new List<string> { "(no error)" }
      : Errors.Select( e => e.ToString() );

    return string.Join( "\n", errors );
  }
}