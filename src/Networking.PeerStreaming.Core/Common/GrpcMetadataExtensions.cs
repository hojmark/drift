using Drift.Domain;
using Grpc.Core;

namespace Drift.Networking.PeerStreaming.Core.Common;

internal static class GrpcMetadataExtensions {
  internal static AgentId GetAgentId( this Metadata metadata ) {
    var v = metadata.Get( "agent-id" );

    if ( v == null ) {
      throw new Exception( "AgentId not found in gRPC metadata" );
    }

    return new AgentId( v.Value );
  }
}