namespace Drift.Coordinator.Api.Control.Dtos;

public sealed record AgentStateDto(
  string Id,
  Uri Address,
  AgentEnrollmentStatusDto EnrollmentStatus,
  AgentConnectionStatusDto ConnectionStatus
);