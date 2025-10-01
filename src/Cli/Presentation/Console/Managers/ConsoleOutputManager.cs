using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Console.Managers.Outputs;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Presentation.Console.Managers;

internal class ConsoleOutputManager(
  //TODO mixed parameter levels
  ILogger consoleLogger,
  TextWriter normalStdOut,
  TextWriter normalErrOut,
  bool normalVerbose,
  bool normalVeryVerbose,
  OutputFormat outputFormat,
  bool plainConsole,
  TextReader reader
) : IOutputManager {
  public TextReader GetReader() {
    return reader;
  }

  public ILogOutput Log {
    get;
  } = new LogOutput( consoleLogger );

  public INormalOutput Normal {
    get;
  } = new NormalOutput( normalStdOut, normalErrOut, plainConsole, normalVerbose, normalVeryVerbose );

  public IJsonOutput Json {
    get;
  } = default!;

  // Could work, but need async option
  public void WithNormalOutput( Action<INormalOutput> action ) {
    if ( outputFormat == OutputFormat.Normal ) {
      action( Normal );
    }
  }

  // Could work, but need async option
  public void WithLogOutput( Action<ILogOutput> action ) {
    if ( outputFormat == OutputFormat.Log ) {
      action( Log );
    }
  }

  // Could work, but need async option
  public void WithJsonOutput( Action<IJsonOutput> action ) {
    // TODO implement
    /*if ( outputFormat == GlobalParameters.OutputFormat.Json ) {
      action( Json );
    }*/
  }

  public bool Is( OutputFormat format ) {
    return outputFormat == format;
  }
}