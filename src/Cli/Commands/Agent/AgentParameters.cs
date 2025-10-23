using System.CommandLine;
using Drift.Cli.Commands.Common;

namespace Drift.Cli.Commands.Agent;

internal record AgentParameters : BaseParameters {
  internal AgentParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}