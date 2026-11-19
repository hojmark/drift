using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Drift.Coordinator.Api;

internal sealed class CoordinatorOpenApiTransformer : IOpenApiDocumentTransformer {
  public Task TransformAsync(
    OpenApiDocument document,
    OpenApiDocumentTransformerContext context,
    CancellationToken cancellationToken
  ) {
    document.Info.Title = "drift | v1";
    document.Servers = [new OpenApiServer { Url = "/" }];
    return Task.CompletedTask;
  }
}