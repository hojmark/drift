using Drift.Cli.Commands.Global;
using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;

namespace Drift.Cli.Output;

internal class ConsoleOutputManager(
  //TODO mixed parameter levels
  ILogger consoleLogger,
  TextWriter stdOut,
  TextWriter errOut,
  bool consoleOutsVerbose,
  GlobalParameters.OutputFormat outputFormat
) : IOutputManager {
  public ILogOutput Log {
    get;
  } = new LogOutput( consoleLogger );

  public INormalOutput Normal {
    get;
  } = new NormalOutput( stdOut, errOut, consoleOutsVerbose );

  public IJsonOutput Json {
    get;
  } = default!;

  // Could work, but need async option
  public void WithNormalOutput( Action<INormalOutput> action ) {
    if ( outputFormat == GlobalParameters.OutputFormat.Normal ) {
      action( Normal );
    }
  }

  // Could work, but need async option
  public void WithLogOutput( Action<ILogOutput> action ) {
    if ( outputFormat == GlobalParameters.OutputFormat.Log ) {
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

  public bool Is( GlobalParameters.OutputFormat format ) {
    return outputFormat == format;
  }
}