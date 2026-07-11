using Drift.Networking.Core.Abstractions;
using Drift.Networking.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Drift.Networking.Server;

// Handle incoming connections (AKA server-side).
internal sealed class InboundMessageService( IMessageStreamManager messageStreamManager, ILogger logger )
  : MessagingService.MessagingServiceBase {
  public override async Task Connect(
    IAsyncStreamReader<Message> requestStream,
    IServerStreamWriter<Message> responseStream,
    ServerCallContext context
  ) {
    try {
      logger.LogInformation( "Inbound stream starting..." );
      var stream = messageStreamManager.Create( requestStream, responseStream, context );
      logger.LogInformation( "Stream #{StreamNo} created", stream.InstanceNo );

      // The stream is closed when the method returns.
      // We thus wait for the read loop to complete (meaning that this client is no longer interested in the stream).
      await stream.ReadTask;

      logger.LogInformation( "Stream #{StreamNo} completed", stream.InstanceNo );
    }
    catch ( Exception ex ) {
      logger.LogError( ex, "Inbound stream failed" );
    }
  }
}