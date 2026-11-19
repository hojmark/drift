using Microsoft.Extensions.Options;

namespace Drift.Hosting.Configuration;

public static class OptionsValidation {
  /// <summary>
  /// Throws an <see cref="OptionsValidationException"/> listing all <paramref name="failures"/>, if any.
  /// </summary>
  /// <typeparam name="TOptions">The options type that was validated.</typeparam>
  public static void ThrowIfAny<TOptions>( IReadOnlyCollection<string> failures ) {
    if ( failures.Count > 0 ) {
      throw new OptionsValidationException( Options.DefaultName, typeof(TOptions), failures );
    }
  }
}