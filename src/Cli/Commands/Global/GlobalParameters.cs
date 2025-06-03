using System.CommandLine;

namespace Drift.Cli.Commands.Global;

internal static class GlobalParameters {
  internal static class Arguments {
    private static readonly Argument<FileInfo> SpecBase =
      new() {
        Name = "spec", //TODO Needed?
        Description = "The network spec file to process.",
        Arity = ArgumentArity.ExactlyOne,
        HelpName = "SPEC"
      };

    internal static readonly Argument<FileInfo> SpecRequired =
      new() {
        Name = SpecBase.Name,
        Description = SpecBase.Description,
        Arity = ArgumentArity.ExactlyOne,
        HelpName = SpecBase.HelpName
      };

    internal static readonly Argument<FileInfo?> SpecOptional =
      new() {
        Name = SpecBase.Name,
        Description = SpecBase.Description,
        Arity = ArgumentArity.ZeroOrOne,
        HelpName = SpecBase.HelpName
      };
  }

  internal static class Options {
    internal static readonly Option<bool> Verbose = new(["--verbose", "-v"], "Verbose output."); // == debug?

    // == trace?
    //internal static readonly Option<bool> VeryVerbose =
    //  new(["--very-verbose", "-vv"], "Very verbose output.");

    internal static readonly Option<OutputFormat> OutputFormatOption =
      new(["--output", "-o"], () => OutputFormat.Normal, "Output format.") {
        IsRequired = false, Arity = ArgumentArity.ExactlyOne,
      };
  }

  // TODO consider grep?
  /// <summary>
  /// Formats for console output.
  /// </summary>
  internal enum OutputFormat {
    /// <summary>
    /// Standard console output (default).
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Log-style console output.
    /// </summary>
    Log = 2

    //TODO support
    /// <summary>
    /// JSON format console output.
    /// </summary>
    //Json = 3,
  }
}