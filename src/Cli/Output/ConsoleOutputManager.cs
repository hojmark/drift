using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Output;

internal class ConsoleOutputManager(
  //TODO mixed parameter levels
  ILogger consoleLogger,
  TextWriter normalStdOut,
  TextWriter normalErrOut,
  bool normalVerbose,
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
  } = new NormalOutput( normalStdOut, normalErrOut, plainConsole, normalVerbose );

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