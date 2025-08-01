using System.CommandLine;
using Drift.Cli.Output;

namespace Drift.Cli.Commands.Common;

/// <summary>
/// Arguments and options shared across commands.
/// </summary>
internal static class CommonParameters {
  /// <summary>
  /// Arguments shared across commands.
  /// </summary>
  internal static class Arguments {
    internal static readonly Argument<FileInfo?> SpecOptional = new("spec") {
      Description = "The network spec file to process.", Arity = ArgumentArity.ZeroOrOne
    };
  }

  /// <summary>
  /// Options shared across commands.
  /// </summary>
  internal static class Options {
    internal static readonly Option<bool>
      Verbose = new("--verbose", "-v") { Description = "Verbose output" }; // == debug?

    // == trace?
    //internal static readonly Option<bool> VeryVerbose =
    //  new(["--very-verbose", "-vv"], "Very verbose output.");

    internal static readonly Option<OutputFormat> OutputFormatOption =
      new("--output", "-o") {
        DefaultValueFactory = _ => OutputFormat.Normal,
        Description = "Output format",
        Required = false,
        Arity = ArgumentArity.ExactlyOne,
      };
  }
}