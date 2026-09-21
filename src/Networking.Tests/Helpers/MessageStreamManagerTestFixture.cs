using Drift.Networking.Client;
using Drift.Networking.Core;
using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Drift.Networking.Tests.Helpers;

internal sealed class MessageStreamManagerTestFixture : IAsyncDisposable {
  private readonly CancellationTokenSource _stopping = new();

  public MessageStreamManagerTestFixture( ILogger? logger = null ) {
    logger ??= NullLogger.Instance;
    MessageHandler = new TestRequestHandler( logger );
    var services = new ServiceCollection();
    services.AddSingleton( logger );
    services.AddSingleton<IMessageHandler>( MessageHandler );
    services.AddMessagingCore( new MessagingOptions { StoppingToken = _stopping.Token } );
    services.AddMessagingClient();
    Services = services.BuildServiceProvider();
    Manager = (MessageStreamManager) Services.GetRequiredService<IMessageStreamManager>();
  }

  public ServiceProvider Services {
    get;
  }

  public MessageStreamManager Manager {
    get;
  }

  public TestRequestHandler MessageHandler {
    get;
  }

  public InboundConnection OpenInbound( string agentId, IMessageStreamManager? manager = null ) {
    var context = TestServerCallContext.Create( cancellationToken: CancellationToken.None );
    context.RequestHeaders.Add( "agent-id", agentId );
    var streams = context.CreateDuplexStreams();
    var connection = ( manager ?? Manager ).Create(
      streams.Server.RequestStream,
      streams.Server.ResponseStream,
      context
    );
    return new InboundConnection( streams.Client.RequestStream, connection );
  }

  public Task StopAsync() => _stopping.CancelAsync();

  public async ValueTask DisposeAsync() {
    await StopAsync();
    await Services.DisposeAsync();
    _stopping.Dispose();
  }
}

internal sealed record InboundConnection(
  IClientStreamWriter<Message> RequestStream,
  IMessageStreamConnection Connection
) {
  public Task Completion => Connection.Completion;
  public IMessageStream Stream => Connection.Stream;
  public Task CompleteAsync() => RequestStream.CompleteAsync();
}