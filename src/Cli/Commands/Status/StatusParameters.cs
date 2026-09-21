using System.CommandLine;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Status;

internal sealed record StatusParameters : BaseParameters {
  internal StatusParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}