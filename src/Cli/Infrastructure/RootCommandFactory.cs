using System.CommandLine;
using System.CommandLine.Help;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Drift.Agent.PeerProtocol;
using Drift.Cli.Commands.Agent;
using Drift.Cli.Commands.Agent.Subcommands.Start;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Help;
using Drift.Cli.Commands.Init;
using Drift.Cli.Commands.Lint;
using Drift.Cli.Commands.Scan;
using Drift.Cli.Presentation.Console;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.Presentation.Rendering;
using Drift.Cli.SpecFile;
using Drift.Domain.ExecutionEnvironment;
using Drift.Domain.Scan;
using Drift.Networking.Clustering;
using Drift.Networking.PeerStreaming.Client;
using Drift.Networking.PeerStreaming.Core;
using Drift.Scanning;
using Drift.Scanning.Scanners;
using Drift.Scanning.Subnets.Interface;

namespace Drift.Cli.Infrastructure;

internal static class RootCommandFactory {
  // Note: registering commands using reflection does not work with AOT compilation
  internal readonly record struct CommandRegistration(
    [property: DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors )]
    Type Handler,
    Func<IServiceProvider, Command> Factory
  ) {
    // ILLink ALSO needs the annotation applied to the out parameter in Deconstruct()
    public void Deconstruct(
      [DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors )]
      out Type handler,
      out Func<IServiceProvider, Command> factory
    ) {
      handler = Handler;
      factory = Factory;
    }
  }

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
    ConfigureExecutionEnvironment( services );
    ConfigureOutput( services, toConsole, plainConsole );
    ConfigureSpecProvider( services );
    ConfigureSubnetProvider( services );
    ConfigureNetworkScanner( services );
    ConfigureAgentCluster( services );
  }

  private static void ConfigureAgentCluster( IServiceCollection services ) {
    services.AddPeerStreamingCore( new PeerStreamingOptions {
      MessageAssembly = typeof(PeerProtocolAssemblyMarker).Assembly
    } );
    services.AddPeerStreamingClient();
    services.AddClustering();
  }

  private static void ConfigureExecutionEnvironment( IServiceCollection services ) {
    services.AddSingleton<IExecutionEnvironmentProvider, CurrentExecutionEnvironmentProvider>();
  }

  private static RootCommand CreateRootCommand( IServiceProvider provider ) {
    // TODO 'from' or 'against'?
    var rootCommand =
      new RootCommand( $"{Chars.SatelliteAntenna} Drift CLI — monitor network drift against your declared state" ) {
        new InitCommand( provider ),
        new ScanCommand( provider ),
        new LintCommand( provider ),
        new AgentCommand( provider )
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
    // Note: since ILogger is scoped, singletons cannot access logging via DI
    services.AddScoped<ILogger>( sp => sp.GetRequiredService<IOutputManager>().GetLogger() );
  }

  private static void ConfigureSpecProvider( IServiceCollection services ) {
    services.AddScoped<ISpecFileProvider, FileSystemSpecProvider>();
  }

  public static void ConfigureSubnetProvider( IServiceCollection services ) {
    services.AddScoped<IInterfaceSubnetProvider, PhysicalInterfaceSubnetProvider>();
  }

  private static void ConfigureBuiltInCommandHandlers( IServiceCollection services ) {
    services.AddScoped<InitCommandHandler>();
    services.AddScoped<ScanCommandHandler>();
    services.AddScoped<LintCommandHandler>();
    services.AddScoped<AgentStartCommandHandler>();
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
    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      services.AddSingleton<IPingTool, LinuxPingTool>();
    }
    else {
      throw new PlatformNotSupportedException();
    }

    services.AddScoped<ISubnetScannerFactory, DefaultSubnetScannerFactory>();
    services.AddScoped<INetworkScanner, DefaultNetworkScanner>();
  }

  private static void AddFigletHeaderToHelpCommand( RootCommand rootCommand ) {
    // rootCommand.Add(CommonParameters.Options.OutputFormat );
    foreach ( var t in rootCommand.Options ) {
      // Update the default HelpOption of the RootCommand
      if ( t is HelpOption defaultHelpOption ) {
        defaultHelpOption.Action = new FigletHeaderHelpAction( (HelpAction) defaultHelpOption.Action! );
        break;
      }
    }
  }
}