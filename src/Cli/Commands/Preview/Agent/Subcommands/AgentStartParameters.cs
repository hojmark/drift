using System.CommandLine;
using Drift.Cli.Commands.Common;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands;

internal record AgentStartParameters : DefaultParameters {
  internal AgentStartParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}