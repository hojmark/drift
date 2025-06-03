using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Drift.Cli;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Scan;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;

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

await Bootstrapper.BootstrapAsync();

//TODO 'from' or 'against'?
var rootCommand = new RootCommand( "📡\uFE0F Drift CLI — monitor network drift against your declared state" );

rootCommand.AddCommand( new InitCommand( loggerFactory ) );
rootCommand.AddCommand( new ScanCommand( loggerFactory ) );

//return await rootCommand.InvokeAsync( args );

var parser = new CommandLineBuilder( rootCommand )
  .UseHelp( ctx => ctx.HelpBuilder.CustomizeLayout( _ => {
        if ( ctx.Command == rootCommand ) {
          return HelpBuilder.Default
            .GetLayout()
            // .Skip( 1 ) // Skip description section
            .Prepend( _ => {
              Spectre.Console.AnsiConsole.Write(
                new FigletText( FigletFont.Load( EmbeddedResourceProvider.GetStream( "small.flf" ) ), "Drift" ) );
              //  Console.WriteLine( "Monitor network drift against your declared state." );
            } );
        }

        return HelpBuilder.Default.GetLayout();
      }
    )
  )
  // TODO support examples in help
  //.UseHelpBuilder( ctx => new CustomHelpBuilder() )
  .UseDefaults()
  .Build();

return await parser.InvokeAsync( args );