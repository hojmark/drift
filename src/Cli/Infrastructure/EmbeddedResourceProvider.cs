using System.Reflection;
using Drift.Common.EmbeddedResources;

namespace Drift.Cli.Infrastructure;

internal class EmbeddedResourceProvider : EmbeddedResourceProviderBase {
  protected override Assembly ResourceAssembly => typeof(EmbeddedResourceProvider).Assembly;
  protected override string RootNamespace => "Drift.Cli";
}