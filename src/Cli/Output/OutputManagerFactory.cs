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

public interface IOutputManagerFactory {
  IOutputManager Create( ParseResult result );
}

//TODO temporary migration away from BinderBase
// Justification: this class is part of providing the alternative to the banned APIs
[SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs" )]
internal class OutputManagerFactory(
  //TODO Inject config instead
  bool toConsole = true
) : IOutputManagerFactory {
  public IOutputManager Create( ParseResult parseResult ) {
    var consoleOuts = GetConsoleOuts( parseResult );
    return new ConsoleOutputManager(
      GetLogger( parseResult, toConsole ),
      consoleOuts.StdOut,
      consoleOuts.ErrOut,
      consoleOuts.Verbose,
      consoleOuts.OutputFormat
    );
  }

  private static (TextWriter StdOut, TextWriter ErrOut, bool Verbose, OutputFormat OutputFormat)
    GetConsoleOuts( ParseResult parseResult ) {
    var outputFormatValue = parseResult.GetValue( CommonParameters.Options.OutputFormatOption );
    var verboseValue = parseResult.GetValue( CommonParameters.Options.Verbose );
    //var veryVerboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.VeryVerbose );

    // Even though the option has a default value, it is not set when the option is not added to a command.
    // Instead, we get 0. It indicates a developer mistake.
    if ( outputFormatValue == 0 ) {
      // throw new Exception( "Output format not specified" );
      // Be graceful for now...
      outputFormatValue = OutputFormat.Normal;
    }

    if ( outputFormatValue is not OutputFormat.Normal ) {
      return ( TextWriter.Null, TextWriter.Null, false, outputFormatValue );
    }

    var consoleOut = parseResult.Configuration.Output;
    var consoleErr = parseResult.Configuration.Error;

    var tempOutputManager = new ConsoleOutputManager(
      NullLogger.Instance,
      consoleOut,
      consoleErr,
      verboseValue /*|| veryVerboseValue*/,
      outputFormatValue
    );

    tempOutputManager.Normal.WriteLineVerbose(
      $"Output format is 'Normal' using '{( /*veryVerboseValue ? "Very Verbose" :*/ "Verbose" )}' output" );

    return ( consoleOut, consoleErr, verboseValue /*|| veryVerboseValue*/, outputFormatValue );
  }

  private static ILogger GetLogger( ParseResult parseResult, bool toConsole ) {
    // Default console rneder uses `OutputTemplateRenderer`, which is internal. Only differnece detected is that MessageTemplateTextFormatter auto single quotes  non strings e.g. enum values.
    // Fix is to call myEnum.ToString()
    var formatter = new MessageTemplateTextFormatter(
      "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    );

    var loggerConfig = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .Enrich.FromLogContext();

    if ( toConsole ) {
      loggerConfig.WriteTo.Console();
    }
    else {
      loggerConfig.WriteTo.Logger( lc => lc
          .Filter.ByIncludingOnly( le => le.Level < LogEventLevel.Error )
          .WriteTo.TextWriter(
            textWriter: parseResult.Configuration.Output,
            formatter: formatter
            //outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
        )
        .WriteTo.Logger( lc => lc
          .Filter.ByIncludingOnly( le => le.Level >= LogEventLevel.Error )
          .WriteTo.TextWriter(
            textWriter: parseResult.Configuration.Error,
            formatter: formatter
            //outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          )
        );
    }

    var loggerFactory = LoggerFactory.Create( builder => builder.AddSerilog( loggerConfig.CreateLogger() )
        .SetMinimumLevel( LogLevel.Debug ) // Parse from args?
    );

    var outputFormatValue = parseResult.GetValue( CommonParameters.Options.OutputFormatOption );
    var verboseValue = parseResult.GetValue( CommonParameters.Options.Verbose );
    //var veryVerboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.VeryVerbose );

    if ( outputFormatValue is not OutputFormat.Log ) {
      return NullLogger.Instance;
    }

    //TODO log level currently broken!
    var loglevel =
      //veryVerboseValue ? LogLevel.Trace :
      verboseValue ? LogLevel.Debug : LogLevel.Information;

    //TODO still getting '[0]' in the output. Should probably create custom logger.
    var logger = loggerFactory.CreateLogger( "" );

    logger.LogDebug( "Output format is '{OutputFormat}' using log level '{LogLevel}'", outputFormatValue.ToString(),
      loglevel.ToString() );

    return logger;
  }
}