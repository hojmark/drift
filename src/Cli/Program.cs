using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Drift.Cli;
using Spectre.Console;

await Bootstrapper.BootstrapAsync();


var rootCommand = RootCommandFactory.Create();

//return await rootCommand.InvokeAsync( args );

var parser = new CommandLineBuilder( rootCommand )
  .UseHelp( ctx => ctx.HelpBuilder.CustomizeLayout( _ => {
        if ( ctx.Command == rootCommand ) {
          return HelpBuilder.Default
            .GetLayout()
            // .Skip( 1 ) // Skip description section
            .Prepend( _ => {
              AnsiConsole.Write(
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