using System.CommandLine;

namespace Drift.Cli.Commands.Common;

internal class ParseResultHolder {
  private ParseResult? _parseResult;

  public ParseResult ParseResult {
    get => _parseResult ??
           throw new InvalidOperationException(
             $"{nameof(ParseResult)} is null. This should have been set during dependency injection."
           );
    set => _parseResult = value;
  }
}