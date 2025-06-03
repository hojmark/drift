using Drift.Cli.Commands.Global;

namespace Drift.Cli.Output.Abstractions;

internal interface IOutputManager {
  ILogOutput Log {
    get;
  }

  INormalOutput Normal {
    get;
  }

  IJsonOutput Json {
    get;
  }

  void WithNormalOutput( Action<INormalOutput> output );
  void WithLogOutput( Action<ILogOutput> output );
  void WithJsonOutput( Action<IJsonOutput> output );

  //TODO hack
  bool Is( GlobalParameters.OutputFormat outputFormat );
}