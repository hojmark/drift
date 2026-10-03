using System.Text.Json.Serialization;
using Drift.Domain;
using Drift.Serialization.Converters;

namespace Drift.Coordinator.Client.Models;

public sealed record CoordinatorAgentStatus(
  [property: JsonConverter( typeof(AgentIdConverter) )]
  AgentId Id,
  Uri Address,
  AgentEnrollmentStatus EnrollmentStatus,
  AgentConnectionStatus ConnectionStatus
);