using System.CommandLine;
using Drift.Cli.Commands.Common;

namespace Drift.Cli.Commands.Preview.Agent;

internal record AgentParameters : DefaultParameters {
  internal AgentParameters( ParseResult parseResult ) : base( parseResult ) {
  }
}