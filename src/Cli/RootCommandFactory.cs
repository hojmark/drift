using System.CommandLine;
using System.CommandLine.Parsing;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Commands.Scan;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Drift.Cli;

internal static class RootCommandFactory {
  internal static Parser CreateParser() {
    return new Parser( Create() );
  }

  internal static RootCommand Create() {
    //TODO consider host pattern

    var loggerConfig = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .Enrich.FromLogContext()
      .WriteTo.Console();

    Log.Logger = loggerConfig
      .CreateLogger();

    var loggerFactory = LoggerFactory.Create( builder => builder.AddSerilog()
        .SetMinimumLevel( LogLevel.Debug ) // Parse from args?
      /*.AddSimpleConsole( config => {
        config.SingleLine = true;
        config.TimestampFormat = "[HH:mm:ss.ffff] ";
      } )*/
    );

    //TODO 'from' or 'against'?
    var rootCommand = new RootCommand( "📡\uFE0F Drift CLI — monitor network drift against your declared state" );

    rootCommand.AddCommand( new InitCommand( loggerFactory ) );
    rootCommand.AddCommand( new ScanCommand( loggerFactory ) );
    rootCommand.AddCommand( new LintCommand( loggerFactory ) );

    return rootCommand;
  }
}