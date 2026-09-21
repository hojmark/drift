using Drift.Domain;

namespace Drift.Coordinator.Services.Agents.Enrollment;

public sealed record AgentEnrollmentRecord( AgentId Id, string Address, DateTimeOffset EnrolledAt );