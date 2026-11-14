using Microsoft.Extensions.DependencyInjection;

namespace Drift.Cli.Infrastructure;

internal sealed record AgentHostServiceConfiguration( Action<IServiceCollection> Configure );

internal sealed record CoordinatorHostServiceConfiguration( Action<IServiceCollection> Configure );