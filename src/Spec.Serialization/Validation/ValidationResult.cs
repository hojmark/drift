namespace Drift.Spec.Serialization.Validation;

public class ValidationResult {
  public bool IsValid {
    get;
    set;
  }

  public List<ValidationError> Errors {
    get;
    set;
  } = new();

  public override string ToString() {
    return string.Join( "\n",
      Errors.Count == 0
        ? new List<string> { "(no error)" }
        : Errors.Select( e => e.ToString() )
    );
  }
}