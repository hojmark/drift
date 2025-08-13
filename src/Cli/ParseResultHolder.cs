using System.CommandLine;

namespace Drift.Cli;

public class ParseResultHolder {
  private ParseResult? _parseResult;

  public ParseResult ParseResult {
    get => _parseResult ??
           throw new InvalidOperationException(
             $"{nameof(ParseResult)} is null. This should have been set via dependency injection."
           );
    set => _parseResult = value;
  }
}