using System.Reflection;
using Drift.Common.EmbeddedResources;

namespace Drift.Spec;

internal class EmbeddedResourceProvider : EmbeddedResourceProviderBase {
  protected override Assembly ResourceAssembly => typeof(EmbeddedResourceProvider).Assembly;
}