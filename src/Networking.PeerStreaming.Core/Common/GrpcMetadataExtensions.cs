using Drift.Domain;
using Grpc.Core;

namespace Drift.Networking.PeerStreaming.Core.Common;

internal static class GrpcMetadataExtensions {
  internal static AgentId GetAgentId( this Metadata metadata ) {
    return new AgentId( metadata.Get( "agent-id" ).Value );
  }
}