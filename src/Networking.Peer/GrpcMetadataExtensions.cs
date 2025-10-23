using Drift.Domain;
using Grpc.Core;

namespace Drift.Networking.Peer;

public static class GrpcMetadataExtensions {
  public static AgentId GetAgentId( this Metadata metadata ) {
    return new AgentId( metadata.Get( "agent-id" ).Value );
  }
}