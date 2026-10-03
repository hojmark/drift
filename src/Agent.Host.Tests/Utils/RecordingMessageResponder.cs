using Drift.Networking.Core.Abstractions;

namespace Drift.Agent.Host.Tests.Utils;

internal class RecordingMessageResponder<TResponse> : IMessageResponder<TResponse>
  where TResponse : IResponse {
  public List<TResponse> Responses {
    get;
  } = [];

  public Task SendAsync( TResponse response ) {
    Responses.Add( response );
    return Task.CompletedTask;
  }
}