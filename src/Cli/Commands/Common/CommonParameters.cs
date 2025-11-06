using System.CommandLine;
using Drift.Cli.Presentation.Console;
using Drift.Cli.Settings;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Commands.Common;

/// <summary>
/// Arguments and options shared across commands.
/// </summary>
internal static class CommonParameters {
  /// <summary>
  /// Arguments shared across commands.
  /// </summary>
  internal static class Arguments {
    internal static readonly Argument<FileInfo?> Spec = new("spec") {
      // TODO different commands warrant different descriptions
      Description = "The network spec file to process.", Arity = ArgumentArity.ZeroOrOne
    };
  }

  /// <summary>
  /// Options shared across commands.
  /// </summary>
  internal static class Options {
    /// <summary>
    /// Enable detailed output, corresponding to <see cref="LogLevel.Debug"/> log level.
    /// </summary>
    internal static readonly Option<bool> Verbose = new("--verbose", "-v") {
      Description = "Verbose output", Arity = ArgumentArity.Zero
    };

    /// <summary>
    /// Enable the most detailed output available, corresponding to <see cref="LogLevel.Trace"/> log level.
    /// </summary>
    internal static readonly Option<bool> VeryVerbose = new("--very-verbose", "-vv") {
      Description = "Very verbose output", Arity = ArgumentArity.Zero, Hidden = true
    };

    internal static readonly Option<OutputFormat> OutputFormat =
      new("--output", "-o") {
        DefaultValueFactory = _ => CliSettings.Load().Appearance.OutputFormat.ToOutputFormat(),
        Description = "Output format",
        Required = false,
        Arity = ArgumentArity.ExactlyOne
      };
  }
}