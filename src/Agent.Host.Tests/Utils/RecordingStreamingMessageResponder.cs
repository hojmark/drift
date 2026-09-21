using Drift.Networking.Core.Abstractions;

namespace Drift.Agent.Host.Tests.Utils;

internal sealed class RecordingStreamingMessageResponder<TProgress, TResponse>
  : RecordingMessageResponder<TResponse>, IStreamingMessageResponder<TProgress, TResponse>
  where TProgress : IResponse
  where TResponse : IResponse {
  public List<TProgress> Progress {
    get;
  } = [];

  public void SendProgress( TProgress progress ) {
    Progress.Add( progress );
  }
}