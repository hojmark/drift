using System.CommandLine;
using Drift.Cli.Abstractions;
using Drift.Cli.Commands.Common;
using Drift.Cli.Commands.Preview.Agent.Subcommands.Utils;
using Drift.Cli.Presentation.Console.Logging;
using Drift.Cli.Presentation.Console.Managers.Abstractions;
using Drift.Cli.SpecFile;
using Drift.Domain;
using Drift.Networking.Grpc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using PeerService = Drift.Cli.Commands.Preview.Agent.Subcommands.Utils.PeerService;

namespace Drift.Cli.Commands.Preview.Agent.Subcommands;

internal class AgentStartCommand : CommandBase<AgentStartParameters, AgentStartCommandHandler> {
  internal AgentStartCommand( IServiceProvider provider )
    : base( "start", "Start a local Drift agent process", provider ) {
    Options.Add( AgentStartParameters.Options.Port );
    Options.Add( AgentStartParameters.Options.Adoptable );
    Options.Add( AgentStartParameters.Options.Join );
  }

  protected override AgentStartParameters CreateParameters( ParseResult result ) {
    return new AgentStartParameters( result );
  }
}

internal class AgentStartCommandHandler(
  IOutputManager output,
  ISpecFileProvider specProvider
) : ICommandHandler<AgentStartParameters> {
  public async Task<int> Invoke( AgentStartParameters parameters, CancellationToken cancellationToken ) {
    output.Log.LogDebug( "Running 'agent start' command" );
    var logger = output.GetLogger();

    var identity = LoadAgentIdentity();

    if ( identity == null ) {
      logger.LogDebug( "Agent is not enrolled" );

      var enrollmentRequest = new EnrollmentRequest( parameters.Adoptable, parameters.Join );
      logger.LogInformation( "Agent cluster enrollment method is {EnrollmentMethod}", enrollmentRequest.Method );
    }
    else {
      logger.LogDebug( "Agent is enrolled into cluster 'milkyway'" );
      logger.LogInformation( "Attempting to re-join cluster 'milkyway'..." );
    }

    /*Inventory? inventory;

    try {
      inventory = await specProvider.GetDeserializedAsync( parameters.SpecFile );
    }
    catch ( FileNotFoundException ) {
      return ExitCodes.GeneralError;
    }*/

    output.Log.LogDebug( "Starting agent..." );

    var builder = WebApplication.CreateBuilder();

    builder.Logging.ClearProviders();
    //builder.Logging.AddProvider( new PredefinedLoggerProvider( output.Log ) );

    builder.Services.AddGrpc();

    builder.Services.AddSingleton<ILogger>( output.GetLogger() );

    builder.Services.AddMessageHandling();
    builder.Services.AddSingleton<AgentPeers>();
    //builder.Services.AddSingleton<Inventory>( inventory );
    builder.Services.AddHostedService<Utils.ConnectionManager>();

    builder.WebHost.ConfigureKestrel( options => {
      options.ListenLocalhost( (int) parameters.Port, o => {
        o.Protocols = HttpProtocols.Http2; // Allow HTTP/2 over plain HTTP
      } );
    } );

    var app = builder.Build();

    app.MapGrpcService<PeerService>();
    // app.MapGrpcReflectionService();
    app.MapGet( "/", () => "Nothing to see here" );

    app.Lifetime.ApplicationStarted.Register( () => {
      output.GetLogger().LogInformation( "Agent started" );
      output.GetLogger().LogInformation( "Listening for incoming connections on port {Port}", parameters.Port );
    } );
    app.Lifetime.ApplicationStopping.Register( () => {
      output.GetLogger().LogInformation( "Agent stopping..." );
    } );
    app.Lifetime.ApplicationStopped.Register( () => {
      output.GetLogger().LogInformation( "Agent stopped" );
    } );

    await app.RunAsync();

    output.Log.LogDebug( "Completed 'agent start' command" );

    return ExitCodes.Success;
  }

  private AgentId? LoadAgentIdentity() {
    if ( false ) {
      return new AgentId( Guid.NewGuid() ); // TODO load from file
    }

    return null;
  }
}

internal class EnrollmentRequest( bool parametersAdoptable, string? parametersJoin ) {
  public EnrollmentMethod Method => parametersAdoptable ? EnrollmentMethod.Adoption : EnrollmentMethod.Jwt;
}

public enum EnrollmentMethod {
  Adoption,
  Jwt
}

public class AgentPeers {
  internal static readonly AgentId Self = new(Guid.NewGuid());
  internal List<AgentId> Peers = [];
}