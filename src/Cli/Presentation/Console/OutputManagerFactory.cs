using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Presentation.Console.Managers;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Common.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Drift.Cli.Presentation.Console;

internal interface IOutputManagerFactory {
  IOutputManager Create( ParseResult result, bool plainConsole );
}

// TODO temporary migration away from BinderBase
[SuppressMessage(
  "ApiDesign",
  "RS0030:Do not use banned APIs",
  Justification = "This class is part of providing the alternative to the banned APIs"
)]
internal class OutputManagerFactory(
  // TODO Inject config instead
  bool toConsole = true
) : IOutputManagerFactory {
  public IOutputManager Create( ParseResult result, bool plainConsole ) {
    var outputFormat = result.GetValue( CommonParameters.Options.OutputFormat );
    var verbose = result.GetValue( CommonParameters.Options.Verbose );
    var veryVerbose = result.GetValue( CommonParameters.Options.VeryVerbose );

    var interactiveOutputOnly = result.GetValue( ScanParameters.Options.Interactive );

    // Even though the option has a default value, it is not set when the option is not added to a command.
    // Instead, we get 0, which indicates a developer mistake.
    if ( outputFormat == 0 ) {
      throw new Exception( "Output format not specified" );
      // // Be graceful for now...
      // outputFormat = OutputFormat.Normal;
    }

    var consoleOut = result.Configuration.Output;
    var consoleErr = result.Configuration.Error;

    return Create( outputFormat, verbose, veryVerbose, interactiveOutputOnly, consoleOut, consoleErr, plainConsole );
  }

  public IOutputManager Create(
    OutputFormat outputFormat,
    bool verbose,
    bool veryVerbose,
    bool interactiveOutputOnly,
    TextWriter consoleOut,
    TextWriter consoleErr,
    bool plainConsole
  ) {
    var bridge = new WriterReaderBridge();
    var outWrapper = new CompoundTextWriter();
    outWrapper.Writers.Add( bridge.Writer );
    if ( !interactiveOutputOnly ) {
      outWrapper.Writers.Add( consoleOut );
    }

    var errWrapper = new CompoundTextWriter();
    errWrapper.Writers.Add( bridge.Writer );
    if ( !interactiveOutputOnly ) {
      errWrapper.Writers.Add( consoleErr );
    }

    var consoleOuts = GetConsoleOuts(
      outputFormat,
      verbose,
      veryVerbose,
      outWrapper,
      errWrapper,
      plainConsole,
      bridge.Reader
    );

    var logger = GetLogger(
      outputFormat,
      verbose,
      veryVerbose,
      outWrapper,
      errWrapper,
      // TODO Get rid of this crappy logic
      !interactiveOutputOnly && toConsole
    );

    return new ConsoleOutputManager(
      logger,
      consoleOuts.StdOut,
      consoleOuts.ErrOut,
      verbose,
      veryVerbose,
      outputFormat,
      plainConsole,
      bridge.Reader
    );
  }

  private static (TextWriter StdOut, TextWriter ErrOut) GetConsoleOuts(
    OutputFormat outputFormat,
    bool verbose,
    bool veryVerbose,
    TextWriter consoleOut,
    TextWriter consoleErr,
    bool plainConsole,
    TextReader outputReader
  ) {
    if ( outputFormat is not OutputFormat.Normal ) {
      return ( TextWriter.Null, TextWriter.Null );
    }

    var tempOutputManager = new ConsoleOutputManager(
      NullLogger.Instance,
      consoleOut,
      consoleErr,
      verbose,
      veryVerbose,
      outputFormat,
      plainConsole,
      outputReader
    );

    tempOutputManager.Normal.WriteLineVerbose( "Output format is 'Normal' using 'Verbose' output" );
    tempOutputManager.Normal.WriteLineVeryVerbose( "Output format is 'Normal' using 'Very Verbose' output" );

    return ( consoleOut, consoleErr );
  }

  private static ILogger GetLogger(
    OutputFormat outputFormatValue,
    bool verbose,
    bool veryVerbose,
    TextWriter consoleOut,
    TextWriter consoleErr,
    // TODO try to remove
    bool toDefaultConsole
  ) {
    if ( outputFormatValue is not OutputFormat.Log ) {
      return NullLogger.Instance;
    }

    var loglevel =
      veryVerbose ? LogLevel.Trace :
      verbose ? LogLevel.Debug : LogLevel.Information;

    // The default Serilog console render uses `OutputTemplateRenderer`, which is internal. The only difference detected is that MessageTemplateTextFormatter auto single quotes  non strings e.g., enum values.
    // Fix is to call myEnum.ToString()
    var formatter = new MessageTemplateTextFormatter(
      "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    );

    var loggerConfig = new LoggerConfiguration()
      .Enrich.FromLogContext();

    if ( verbose ) {
      loggerConfig.MinimumLevel.Debug();
    }
    else {
      loggerConfig.MinimumLevel.Information();
    }

    if ( toDefaultConsole ) {
      loggerConfig.WriteTo.Console();
    }
    else {
      loggerConfig.WriteTo.Logger( lc => lc
          .Filter.ByIncludingOnly( le => le.Level < LogEventLevel.Error )
          .WriteTo.TextWriter(
            textWriter: consoleOut,
            formatter: formatter
            // outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
        )
        .WriteTo.Logger( lc => lc
          .Filter.ByIncludingOnly( le => le.Level >= LogEventLevel.Error )
          .WriteTo.TextWriter(
            textWriter: consoleErr,
            formatter: formatter
            // outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
        );
    }

    var loggerFactory = LoggerFactory.Create( builder => builder.AddSerilog( loggerConfig.CreateLogger() )
        .SetMinimumLevel( LogLevel.Debug ) // Parse from args?
    );

    // TODO still getting '[0]' in the output. Should probably create custom logger.
    var logger = loggerFactory.CreateLogger( string.Empty );

    logger.LogDebug(
      "Output format is '{OutputFormat}' using log level '{LogLevel}'",
      outputFormatValue.ToString(),
      loglevel.ToString()
    );

    return logger;
  }
}