using System.CommandLine;
using System.CommandLine.Help;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Help;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Presentation.Output;
using Drift.Cli.Presentation.Output.Abstractions;
using Drift.Cli.Presentation.Output.Logging;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.SpecFile;
using Drift.Domain.Scan;
using Drift.Scanning;
using Drift.Scanning.Scanners;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Infrastructure;

internal static class RootCommandFactory {
  // Note: registering commands using reflection does not work with AOT compilation
  internal readonly record struct CommandRegistration(
    Type Handler,
    Func<IServiceProvider, Command> Factory
  );

  internal static RootCommand Create(
    bool toConsole,
    bool plainConsole = false,
    Action<IServiceCollection>? configureServices = null,
    CommandRegistration[]? customCommands = null
  ) {
    var services = new ServiceCollection();
    ConfigureDefaults( services, toConsole, plainConsole );
    ConfigureBuiltInCommandHandlers( services );
    ConfigureDynamicCommands( services, customCommands ?? [] );
    configureServices?.Invoke( services );

    var provider = services.BuildServiceProvider();
    var rootCommand = CreateRootCommand( provider );
    ConfigureDynamicCommands( provider, rootCommand, customCommands );

    return rootCommand;
  }

  private static void ConfigureDefaults( IServiceCollection services, bool toConsole, bool plainConsole ) {
    services.AddScoped<ParseResultHolder>();
    ConfigureOutput( services, toConsole, plainConsole );
    ConfigureSpecProvider( services );
    ConfigureSubnetProvider( services );
    ConfigureNetworkScanner( services );
  }

  private static RootCommand CreateRootCommand( IServiceProvider provider ) {
    //TODO 'from' or 'against'?
    var rootCommand =
      new RootCommand( $"{Chars.SatelliteAntenna} Drift CLI — monitor network drift against your declared state" ) {
        new InitCommand( provider ), new ScanCommand( provider ), new LintCommand( provider )
      };

    rootCommand.TreatUnmatchedTokensAsErrors = true;

    AddFigletHeaderToHelpCommand( rootCommand );

    return rootCommand;
  }

  private static void ConfigureOutput( IServiceCollection services, bool toConsole, bool plainConsole ) {
    services.AddSingleton<IOutputManagerFactory>( new OutputManagerFactory( toConsole ) );
    services.AddScoped<IOutputManager>( sp => {
      var holder = sp.GetRequiredService<ParseResultHolder>();
      var parseResult = holder.ParseResult ??
                        throw new InvalidOperationException( $"{nameof(ParseResultHolder.ParseResult)} not set" );
      var factory = sp.GetRequiredService<IOutputManagerFactory>();
      return factory.Create( parseResult, plainConsole );
    } );
  }

  private static void ConfigureSpecProvider( IServiceCollection services ) {
    services.AddScoped<ISpecFileProvider, FileSystemSpecProvider>();
  }

  private static void ConfigureSubnetProvider( IServiceCollection services ) {
    services.AddScoped<IInterfaceSubnetProvider>( sp =>
      new PhysicalInterfaceSubnetProvider( sp.GetRequiredService<IOutputManager>().GetLogger() )
    );
  }

  private static void ConfigureBuiltInCommandHandlers( IServiceCollection services ) {
    services.AddScoped<InitCommandHandler>();
    services.AddScoped<ScanCommandHandler>();
    services.AddScoped<LintCommandHandler>();
  }

  private static void ConfigureDynamicCommands( IServiceCollection services, CommandRegistration[] commands ) {
    foreach ( var (handlerType, _) in commands ) {
      services.AddScoped( handlerType );
    }
  }

  private static void ConfigureDynamicCommands(
    IServiceProvider provider,
    RootCommand rootCommand,
    CommandRegistration[]? commands
  ) {
    foreach ( var registration in commands ?? [] ) {
      rootCommand.Add( registration.Factory( provider ) );
    }
  }

  private static void ConfigureNetworkScanner( IServiceCollection services ) {
    services.AddSingleton<IPingTool, LinuxPingTool>();
    services.AddScoped<SubnetScannerFactory>();
    services.AddScoped<INetworkScanner, DefaultNetworkScanner>();
  }

  private static void AddFigletHeaderToHelpCommand( RootCommand rootCommand ) {
    //rootCommand.Add(CommonParameters.Options.OutputFormat );
    foreach ( var t in rootCommand.Options ) {
      // Update the default HelpOption of the RootCommand
      if ( t is HelpOption defaultHelpOption ) {
        defaultHelpOption.Action = new FigletHeaderHelpAction( (HelpAction) defaultHelpOption.Action! );
        break;
      }
    }
  }
}