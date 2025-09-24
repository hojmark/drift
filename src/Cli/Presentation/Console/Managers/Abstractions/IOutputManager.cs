namespace Drift.Cli.Presentation.Console.Managers.Abstractions;

// Internal?
internal interface IOutputManager {
  internal ILogOutput Log {
    get;
  }

  internal INormalOutput Normal {
    get;
  }

  internal IJsonOutput Json {
    get;
  }

  /// <summary>
  /// Gets a <see cref="TextReader"/> that can be used to read from both the <see cref="Normal"/> and <see cref="Log"/>
  /// outputs, depending on the configured output format.
  /// </summary>
  internal TextReader GetReader();

  internal void WithNormalOutput( Action<INormalOutput> action );

  internal void WithLogOutput( Action<ILogOutput> action );

  internal void WithJsonOutput( Action<IJsonOutput> action );

  // TODO hack
  internal bool Is( OutputFormat format );
}