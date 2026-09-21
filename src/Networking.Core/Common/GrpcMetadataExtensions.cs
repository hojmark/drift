using Drift.Domain;
using Grpc.Core;

namespace Drift.Networking.Core.Common;

internal static class GrpcMetadataExtensions {
  internal static AgentId GetAgentId( this Metadata metadata ) {
    var v = metadata.Get( "agent-id" );

    if ( v == null ) {
      throw new Exception( "AgentId not found in gRPC metadata" );
    }

    return AgentId.Parse( v.Value, null );
  }
}
