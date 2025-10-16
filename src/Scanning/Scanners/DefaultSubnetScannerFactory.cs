using System.Runtime.InteropServices;
using Drift.Domain;
using Drift.Domain.ExecutionEnvironment;
using Drift.Domain.Scan;
using Microsoft.Extensions.Logging;

namespace Drift.Scanning.Scanners;

public class DefaultSubnetScannerFactory(
  IPingTool pingTool,
  ILogger logger,
  IExecutionEnvironmentProvider executionEnvironmentProvider
  /*IAgentClient agentClient,*/
  // IEnumerable<CidrBlock> localSubnets
) : ISubnetScannerFactory {
  private const bool UseFping = false;

  public ISubnetScanner Get( CidrBlock cidr ) {
    /*return localSubnets.Contains( cidr )
      ? new LocalSubnetScanner( _pingTool )
      : new RemoteSubnetScanner( _agentClient );*/

    logger.LogDebug( "Getting subnet scanner for CIDR block {Cidr}", cidr );

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) ) {
      if ( UseFping ) {
        logger.LogDebug( "Using {SubnetScanner} (user preference)", nameof(LinuxFpingSubnetScanner) );
        return new LinuxFpingSubnetScanner();
      }

      var environment = executionEnvironmentProvider.Get();

      switch ( environment ) {
        // TODO fix
        /*case DriftExecutionEnvironment.Container:
          logger.LogDebug(
            "Using {SubnetScanner} (execution environment is {ExecutionEnvironment})",
            nameof(LinuxFpingSubnetScanner),
            environment
          );
          return new LinuxFpingSubnetScanner();*/
        default:
          logger.LogDebug( "Using {SubnetScanner} (default)", nameof(LinuxPingSubnetScanner) );
          return new LinuxPingSubnetScanner( pingTool );
      }
    }

    if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) ) {
      return new WindowsPingSubnetScanner();
    }

    throw new PlatformNotSupportedException();
  }
}