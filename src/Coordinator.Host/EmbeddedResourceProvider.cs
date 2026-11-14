using System.Reflection;
using Drift.Common.EmbeddedResources;

namespace Drift.Coordinator.Host;

internal sealed class EmbeddedResourceProvider : EmbeddedResourceProviderBase {
  protected override Assembly ResourceAssembly => typeof(EmbeddedResourceProvider).Assembly;
}