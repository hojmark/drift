using System.Runtime.InteropServices;
using Drift.Domain.Scan;
using Drift.Scanning.Scanners;
using Drift.Scanning.Scanners.Factories;
using Drift.Scanning.Subnets.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Drift.Scanning;

public static class ServiceCollectionExtensions {
  /// <summary>
  /// Registers the platform-specific scan orchestrator.
  /// </summary>
  public static IServiceCollection AddScanning( this IServiceCollection services ) {
    services.AddScoped<IInterfaceSubnetProvider, PhysicalInterfaceSubnetProvider>();

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      services.AddSingleton<IPingTool, LinuxPingTool>();
    }
    else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      services.AddSingleton<IPingTool, WindowsPingTool>();
    }
    else {
      throw new PlatformNotSupportedException();
    }

    services.AddScoped<ISubnetScannerFactory, LocalSubnetScannerFactory>();
    services.AddScoped<IScanOrchestrator, ScanOrchestrator>();
    return services;
  }
}