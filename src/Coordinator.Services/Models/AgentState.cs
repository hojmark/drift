using Drift.Domain;

namespace Drift.Coordinator.Services.Models;

public sealed record AgentState(
  AgentId Id,
  Uri Address,
  AgentEnrollmentStatus EnrollmentStatus,
  AgentConnectionStatus ConnectionStatus
);