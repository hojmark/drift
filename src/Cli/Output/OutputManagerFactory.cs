using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Drift.Cli.Commands.Common;
using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Drift.Cli.Output;

internal interface IOutputManagerFactory {
  IOutputManager Create( ParseResult result, bool plainConsole );
}

//TODO temporary migration away from BinderBase
// Justification: this class is part of providing the alternative to the banned APIs
[SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs" )]
internal class OutputManagerFactory(
  //TODO Inject config instead
  bool toConsole = true
) : IOutputManagerFactory {
  public IOutputManager Create( ParseResult parseResult, bool plainConsole ) {
    var outputFormat = parseResult.GetValue( CommonParameters.Options.OutputFormat );
    var verbose = parseResult.GetValue( CommonParameters.Options.Verbose );
    //var veryVerboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.VeryVerbose );

    // Even though the option has a default value, it is not set when the option is not added to a command.
    // Instead, we get 0, which indicates a developer mistake.
    if ( outputFormat == 0 ) {
      throw new Exception( "Output format not specified" );
      // // Be graceful for now...
      // outputFormat = OutputFormat.Normal;
    }

    var consoleOut = parseResult.Configuration.Output;
    var consoleErr = parseResult.Configuration.Error;

    return Create( outputFormat, verbose, consoleOut, consoleErr, plainConsole );
  }

  public IOutputManager Create(
    OutputFormat outputFormat,
    bool verbose,
    TextWriter consoleOut,
    TextWriter consoleErr,
    bool plainConsole
  ) {
    var sharedOutput = new WriterReaderBridge();
    var stdOutWrapper = new CompoundTextWriter( consoleOut, sharedOutput.Writer );
    var errOutWrapper = new CompoundTextWriter( consoleErr, sharedOutput.Writer );

    var consoleOuts = GetConsoleOuts(
      outputFormat,
      verbose,
      stdOutWrapper,
      errOutWrapper,
      plainConsole,
      sharedOutput.Reader
    );

    return new ConsoleOutputManager(
      GetLogger( outputFormat, verbose, stdOutWrapper, errOutWrapper, toConsole ),
      consoleOuts.StdOut,
      consoleOuts.ErrOut,
      verbose,
      outputFormat,
      plainConsole,
      sharedOutput.Reader
    );
  }

  private static (TextWriter StdOut, TextWriter ErrOut) GetConsoleOuts( OutputFormat outputFormat,
    bool verboseValue,
    TextWriter consoleOut,
    TextWriter consoleErr,
    bool plainConsole,
    TextReader sharedOutputReader
  ) {
    if ( outputFormat is not OutputFormat.Normal ) {
      return ( TextWriter.Null, TextWriter.Null );
    }

    var tempOutputManager = new ConsoleOutputManager(
      NullLogger.Instance,
      consoleOut,
      consoleErr,
      verboseValue /*|| veryVerboseValue*/,
      outputFormat,
      plainConsole,
      sharedOutputReader
    );

    tempOutputManager.Normal.WriteLineVerbose(
      $"Output format is 'Normal' using '{( /*veryVerboseValue ? "Very Verbose" :*/ "Verbose" )}' output"
    );

    return ( consoleOut, consoleErr );
  }

  private static ILogger GetLogger(
    OutputFormat outputFormatValue,
    bool verboseValue,
    TextWriter consoleOut,
    TextWriter consoleErr,
    // TODO try to remove
    bool toDefaultConsole
  ) {
    if ( outputFormatValue is not OutputFormat.Log ) {
      return NullLogger.Instance;
    }

    var loglevel =
      //veryVerboseValue ? LogLevel.Trace :
      verboseValue ? LogLevel.Debug : LogLevel.Information;

    // Default console rneder uses `OutputTemplateRenderer`, which is internal. Only differnece detected is that MessageTemplateTextFormatter auto single quotes  non strings e.g. enum values.
    // Fix is to call myEnum.ToString()
    var formatter = new MessageTemplateTextFormatter(
      "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    );

    var loggerConfig = new LoggerConfiguration()
      .Enrich.FromLogContext();

    if ( verboseValue ) {
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
            //outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
        )
        .WriteTo.Logger( lc => lc
          .Filter.ByIncludingOnly( le => le.Level >= LogEventLevel.Error )
          .WriteTo.TextWriter(
            textWriter: consoleErr,
            formatter: formatter
            //outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
        );
    }

    var loggerFactory = LoggerFactory.Create( builder => builder.AddSerilog( loggerConfig.CreateLogger() )
        .SetMinimumLevel( LogLevel.Debug ) // Parse from args?
    );

    //TODO still getting '[0]' in the output. Should probably create custom logger.
    var logger = loggerFactory.CreateLogger( "" );

    logger.LogDebug( "Output format is '{OutputFormat}' using log level '{LogLevel}'", outputFormatValue.ToString(),
      loglevel.ToString() );

    return logger;
  }
}