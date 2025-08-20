using Drift.Core.Abstractions;
using Drift.Core.Scan.Subnet;
using Drift.Domain;
using Drift.Domain.NeoProgress;

namespace Drift.Core.Scan;

public static class SubnetDiscovery {
  internal static async Task<List<CidrBlock>> DetermineSubnets(
    ScanRequest request,
    ProgressNodeNew node,
    IInterfaceSubnetProvider interfaceSubnetProvider ) {
    //builder.UpdateStep(DiscoveryStep.SubnetDiscovery, 0, data);
    var fromInterface = node.Add( "Finding from ifs" );
    var fromSpec = node.Add( "Finding from spec" );

    //onProgress.Invoke( builder.Build( "Initializing subnet discovery..." ) );

    var subnetProviders = new List<ISubnetProvider> { interfaceSubnetProvider };
    fromInterface.Complete();
    //onProgress.Invoke( builder.Build( "Found subnets from interfaces: xxx" ) );

    if ( request.Spec != null ) {
      subnetProviders.Add( new DeclaredSubnetProvider( request.Spec.Subnets ) );
    }

    //onProgress.Invoke(
    //builder.Build(
    //"Found subnets from network spec: xxx" ) ); // Warning if some does not seem to be possible to reach given the physical interfaces

    fromSpec.Complete();

    var subnetProvider = new CompositeSubnetProvider( subnetProviders );
    //onProgress.Invoke(
    //  builder.Build(
    //    "Removing duplicates" ) ); // Warning if some does not seem to be possible to reach given the physical interfaces

    var subnets = subnetProvider.Get();
    //onProgress.Invoke( builder.Build( "Found subnets: " + string.Join( ", ", subnets ) ) );

    /* var sources = new List<SubnetSource>();
     var allSubnets = new List<CidrBlock>();

     // Initialize discovery data
     var data = new SubnetDiscoveryData {
       Sources = sources,
       FinalSubnets = allSubnets,
       CurrentActivity = "Starting subnet discovery..."
     };

     builder.UpdateStep(DiscoveryStep.SubnetDiscovery, 0, data);
     onProgress?.Invoke(builder.Build("Initializing subnet discovery..."));

     // Step 1: Discover from network interfaces
     await DiscoverFromInterfaces(sources, allSubnets, builder, onProgress);

     // Step 2: Load declared subnets from configuration
     await LoadDeclaredSubnets(request.Spec, sources, allSubnets, builder, onProgress);

     // Step 3: Clean up and finalize
     RemoveDuplicates(allSubnets);

     // Final update
     var finalData = new SubnetDiscoveryData {
       Sources = sources,
       FinalSubnets = allSubnets,
       CurrentActivity = $"Discovery complete - ready to scan {allSubnets.Count} subnet(s)"
     };

     builder.UpdateStep(DiscoveryStep.SubnetDiscovery, 100, finalData);
     onProgress?.Invoke(builder.Build($"Found {allSubnets.Count} subnet(s) for scanning"));

     return allSubnets;*/
    return subnets;
  }

/*
  private async Task DiscoverFromInterfaces(
    List<SubnetSource> sources,
    List<CidrBlock> allSubnets,
    SubnetDiscoveryData data ) {
    var interfaceSource = new SubnetSource {
      Name = "Network Interfaces", Type = "Interface Discovery", Status = SourceStatus.InProgress, Subnets = []
    };
    sources.Add( interfaceSource );

    // Update progress
    var updatedData = data with { CurrentActivity = "Scanning network interfaces..." };
    builder.UpdateStep( "Subnet Discovery", 25, updatedData );

    try {
      var interfaceSubnets = interfaceSubnetProvider.Get();

      interfaceSource = interfaceSource with { Status = SourceStatus.Completed, Subnets = interfaceSubnets };

      allSubnets.AddRange( interfaceSubnets );

      output.Log.LogInformation( "Found {Count} subnet(s) from interfaces: {Subnets}",
        interfaceSubnets.Count, string.Join( ", ", interfaceSubnets ) );
    }
    catch ( Exception ex ) {
      interfaceSource = interfaceSource with { Status = SourceStatus.Failed, ErrorMessage = ex.Message };
      output.Log.LogError( ex, "Failed to discover interface subnets" );
    }

    // Replace the source in the list
    sources[sources.Count - 1] = interfaceSource;
  }

  private async Task LoadDeclaredSubnets(
    Network? network,
    List<SubnetSource> sources,
    List<CidrBlock> allSubnets,
    SubnetDiscoveryData data ) {
    var declaredSource = new SubnetSource {
      Name = "Declared Subnets",
      Type = "Configuration File",
      Status = network?.Subnets?.Any() == true ? SourceStatus.InProgress : SourceStatus.Skipped,
      Subnets = []
    };
    sources.Add( declaredSource );

    // Update progress
    var updatedData = data with { CurrentActivity = "Loading declared subnets..." };
    builder.UpdateStep( "Subnet Discovery", 50, updatedData );

    if ( network?.Subnets?.Any() != true ) {
      sources[sources.Count - 1] = declaredSource;
      return;
    }

    try {
      var declaredSubnets = network.Subnets
        .Where( s => s.Enabled != false )
        .Select( s => s.Network )
        .ToList();

      declaredSource = declaredSource with { Status = SourceStatus.Completed, Subnets = declaredSubnets };

      allSubnets.AddRange( declaredSubnets );

      output.Log.LogInformation( "Loaded {Count} declared subnet(s): {Subnets}",
        declaredSubnets.Count, string.Join( ", ", declaredSubnets ) );
    }
    catch ( Exception ex ) {
      declaredSource = declaredSource with { Status = SourceStatus.Failed, ErrorMessage = ex.Message };
      output.Log.LogError( ex, "Failed to load declared subnets" );
    }

    sources[sources.Count - 1] = declaredSource;
  }

  private void RemoveDuplicates( List<CidrBlock> subnets ) {
    var originalCount = subnets.Count;
    var uniqueSubnets = subnets.Distinct().ToList();
    subnets.Clear();
    subnets.AddRange( uniqueSubnets );

    if ( originalCount != subnets.Count ) {
      output.Log.LogDebug( "Removed {Count} duplicate subnet(s)", originalCount - subnets.Count );
    }
  }*/
}