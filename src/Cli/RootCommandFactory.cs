using System.CommandLine;
using System.CommandLine.Help;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Output;
using Drift.Cli.Output.Abstractions;
using Drift.Cli.Scan;
using Drift.Cli.Tools;
using Drift.Domain.Scan;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli;

internal static class RootCommandFactory {
  internal static RootCommand Create( bool toConsole, Action<IServiceCollection>? configureServices = null ) {
    var services = new ServiceCollection();

    ConfigureDefaults( services, toConsole );

    // Custom overrides e.g. for testing
    configureServices?.Invoke( services );

    var provider = services.BuildServiceProvider();

    return CreateRootCommand( provider );
  }

  private static void ConfigureDefaults( ServiceCollection services, bool toConsole ) {
    services.AddScoped<ParseResultHolder>();
    ConfigureOutput( services, toConsole );
    ConfigureNetworkScanner( services );
    ConfigureCommandHandlers( services );
  }


  private static RootCommand CreateRootCommand( IServiceProvider provider ) {
    //TODO 'from' or 'against'?
    var rootCommand =
      new RootCommand( "📡\uFE0F Drift CLI — monitor network drift against your declared state" ) {
        new InitCommand( provider ), new ScanCommand( provider ), new LintCommand( provider )
      };

    rootCommand.TreatUnmatchedTokensAsErrors = true;

    AddFigletHeaderToHelpCommand( rootCommand );

    return rootCommand;
  }

  private static void ConfigureOutput( ServiceCollection services, bool toConsole ) {
    services.AddSingleton<IOutputManagerFactory>( new OutputManagerFactory( toConsole ) );
    services.AddScoped<IOutputManager>( sp => {
      var holder = sp.GetRequiredService<ParseResultHolder>();
      var parseResult = holder.ParseResult ??
                        throw new InvalidOperationException( $"{nameof(ParseResultHolder.ParseResult)} not set" );
      var factory = sp.GetRequiredService<IOutputManagerFactory>();
      return factory.Create( parseResult );
    } );
  }

  private static void ConfigureNetworkScanner( ServiceCollection services ) {
    services.AddSingleton<IPingTool, OsPingTool>();
    services.AddScoped<INetworkScanner, PingNetworkScanner>();
  }

  private static void ConfigureCommandHandlers( ServiceCollection services ) {
    services.AddScoped<InitCommandHandler>();
    services.AddScoped<ScanCommandHandler>();
    services.AddScoped<LintCommandHandler>();
  }

  private static void AddFigletHeaderToHelpCommand( RootCommand rootCommand ) {
    foreach ( var t in rootCommand.Options ) {
      // Update the default HelpOption of the RootCommand
      if ( t is HelpOption defaultHelpOption ) {
        defaultHelpOption.Action = new FigletHeaderHelpAction( (HelpAction) defaultHelpOption.Action! );
        break;
      }
    }
  }
}