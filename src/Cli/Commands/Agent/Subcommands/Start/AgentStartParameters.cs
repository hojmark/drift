using System.CommandLine;
using Drift.Cli.Commands.Common.Parameters;

namespace Drift.Cli.Commands.Agent.Subcommands.Start;

internal record AgentStartParameters : BaseParameters {
  internal static class Options {
    internal static readonly Option<bool> Adoptable = new("--adoptable") {
      DefaultValueFactory = _ => false,
      Description = "Allow this agent to be adopted by another peer in the agent cluster"
    };

    // support @ for supplying local file
    internal static readonly Option<string> Join = new("--join") { Description = "Join the agent cluster using a JWT" };

    internal static readonly Option<bool> Daemon = new("--daemon", "-d") {
      Description = "Run the agent as a background daemon"
    };

    internal static readonly Option<ushort> Port = new("--port", "-p") {
      DefaultValueFactory = _ => 51515, Description = "Set the port used for both adoption and communication"
    };
  }

  internal AgentStartParameters( ParseResult parseResult ) : base( parseResult ) {
    Port = parseResult.GetValue( Options.Port );
    Adoptable = parseResult.GetValue( Options.Adoptable );
    Join = parseResult.GetValue( Options.Join );

    if ( !Adoptable && string.IsNullOrWhiteSpace( Join ) ) {
      throw new ArgumentException( "Either --adoptable or --join <token> must be specified." );
    }

    if ( Adoptable && !string.IsNullOrWhiteSpace( Join ) ) {
      throw new ArgumentException( "Cannot specify both --adoptable and --join." );
    }
  }

  public string? Join {
    get;
    set;
  }

  public bool Adoptable {
    get;
    set;
  }

  public ushort Port {
    get;
    set;
  }
}