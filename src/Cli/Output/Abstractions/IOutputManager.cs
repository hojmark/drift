namespace Drift.Cli.Output.Abstractions;

// Internal?
public interface IOutputManager {
  internal ILogOutput Log {
    get;
  }

  internal INormalOutput Normal {
    get;
  }

  internal IJsonOutput Json {
    get;
  }

  internal void WithNormalOutput( Action<INormalOutput> output );
  internal void WithLogOutput( Action<ILogOutput> output );
  internal void WithJsonOutput( Action<IJsonOutput> output );

  //TODO hack
  internal bool Is( OutputFormat outputFormat );
}