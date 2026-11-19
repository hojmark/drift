using Drift.Domain;
using Grpc.Core;

namespace Drift.Networking.Core.Common;

internal static class GrpcMetadataExtensions {
  internal const string AgentIdKey = "agent-id";

  internal static AgentId GetAgentId( this Metadata metadata ) {
    var v = metadata.Get( AgentIdKey );

    if ( v == null ) {
      throw new Exception( $"{AgentIdKey} not found in gRPC metadata" );
    }

    return AgentId.Parse( v.Value, null );
  }
}