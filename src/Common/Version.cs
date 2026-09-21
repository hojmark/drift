using System.Reflection;

namespace Drift.Common;

public static class DriftMetadata {
  public static readonly string Version = Assembly.GetAssembly( typeof(DriftMetadata) )?
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion ?? throw new InvalidOperationException( "Could not determine version" );
}