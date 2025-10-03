using System.Diagnostics.CodeAnalysis;

namespace Drift.Cli;

internal static class BannedSymbols {
  // Remove suppression to verify that all these APIs indeed produce build warnings
  [SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs", Justification = "Exist for test purposes" )]
  private static void BannedInCliProject() {
    Console.WriteLine();
    Console.WriteLine( true );
    Console.WriteLine( 'A' );
    Console.WriteLine( string.Empty );
    Console.WriteLine( 1 );
    Console.WriteLine( 1L );
    Console.WriteLine( new object() );
    Console.WriteLine( "{0}", new object() );

    Console.Write( true );
    Console.Write( 'A' );
    Console.Write( string.Empty );
    Console.Write( 1 );
    Console.Write( 1L );
    Console.Write( new object() );
    Console.Write( "{0}", new object() );

    Console.Out.WriteLine();
    Console.Out.WriteLine( true );
    Console.Out.WriteLine( 'A' );
    Console.Out.WriteLine( string.Empty );
    Console.Out.WriteLine( 1 );
    Console.Out.WriteLine( 1L );
    Console.Out.WriteLine( new object() );
    Console.Out.WriteLine( "{0}", new object() );
    Console.Error.WriteLine();
    Console.Error.WriteLine( true );
    Console.Error.WriteLine( 'A' );
    Console.Error.WriteLine( string.Empty );
    Console.Error.WriteLine( 1 );
    Console.Error.WriteLine( 1L );
    Console.Error.WriteLine( new object() );
    Console.Error.WriteLine( "{0}", new object() );

    _ = Console.Out;
    _ = Console.Error;
  }

  // Remove suppression to verify that all these APIs indeed produce build warnings
  [SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs", Justification = "Exist for test purposes" )]
  private static void BannedInAllProjects() {
    // Method exist for test purposes
  }
}