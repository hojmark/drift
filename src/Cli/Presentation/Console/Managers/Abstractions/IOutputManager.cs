namespace Drift.Cli.Presentation.Console.Managers.Abstractions;

// Internal?
internal interface IOutputManager {
  /// <summary>
  /// Gets a <see cref="TextReader"/> that can be used to read from both the <see cref="Normal"/> and <see cref="Log"/>
  /// outputs, depending on the configured output format.
  /// </summary>
  internal TextReader GetReader();

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