using System.CommandLine.Binding;
using System.CommandLine.IO;
using System.Diagnostics.CodeAnalysis;
using Drift.Cli.Commands.Global;
using Drift.Cli.Output.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Cli.Output;

// Justification: this class is part of providing the alternative to the banned APIs
[SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs" )]
internal class ConsoleOutputManagerBinder( ILoggerFactory loggerFactory ) : BinderBase<IOutputManager> {
  protected override IOutputManager GetBoundValue( BindingContext bindingContext ) {
    var consoleOuts = GetConsoleOuts( bindingContext );
    return new ConsoleOutputManager(
      GetLogger( bindingContext, loggerFactory ),
      consoleOuts.StdOut,
      consoleOuts.ErrOut,
      consoleOuts.Verbose,
      consoleOuts.OutputFormat
    );
  }

  private static (TextWriter StdOut, TextWriter ErrOut, bool Verbose, GlobalParameters.OutputFormat OutputFormat)
    GetConsoleOuts( BindingContext bindingContext ) {
    var outputFormatValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.OutputFormatOption );
    var verboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.Verbose );
    //var veryVerboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.VeryVerbose );

    // Even though the option has a default value, it is not set when the option is not added to a command.
    // Instead, we get 0. It indicates a developer mistake.
    if ( outputFormatValue == 0 ) {
      // throw new Exception( "Output format not specified" );
      // Be graceful for now...
      outputFormatValue = GlobalParameters.OutputFormat.Normal;
    }

    if ( outputFormatValue is not GlobalParameters.OutputFormat.Normal ) {
      return ( TextWriter.Null, TextWriter.Null, false, outputFormatValue );
    }

    var consoleOut = bindingContext.Console.Out.CreateTextWriter();
    var consoleErr = bindingContext.Console.Error.CreateTextWriter();

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

  private static ILogger GetLogger( BindingContext bindingContext, ILoggerFactory loggerFactory2 ) {
    var outputFormatValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.OutputFormatOption );
    var verboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.Verbose );
    //var veryVerboseValue = bindingContext.ParseResult.GetValueForOption( GlobalParameters.Options.VeryVerbose );

    if ( outputFormatValue is not GlobalParameters.OutputFormat.Log ) {
      return NullLogger.Instance;
    }

    //TODO log level currently broken!
    var loglevel =
      //veryVerboseValue ? LogLevel.Trace :
      verboseValue ? LogLevel.Debug : LogLevel.Information;

    /*using var loggerFactory = LoggerFactory.Create( builder => builder
      .SetMinimumLevel( loglevel )
      .AddSimpleConsole( config => {
        config.SingleLine = true;
        config.TimestampFormat = "[HH:mm:ss.ffff] ";
      } ) );*/

    //TODO still getting '[0]' in the output. Should probably create custom logger.
    var logger = loggerFactory2.CreateLogger( "" );

    logger.LogDebug( "Output format is '{OutputFormat}' using log level '{LogLevel}'", outputFormatValue, loglevel );

    return logger;
  }
}