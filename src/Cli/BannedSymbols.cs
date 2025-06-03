using System.Diagnostics.CodeAnalysis;

namespace Drift.Cli;

internal static class BannedSymbols {
  // Remove suppression to verify that all these APIs indeed produce build warnings
  [SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs" )]
  private static void BannedInCliProject() {
    Console.WriteLine();
    Console.WriteLine( true );
    Console.WriteLine( 'A' );
    Console.WriteLine( "" );
    Console.WriteLine( 1 );
    Console.WriteLine( 1L );
    Console.WriteLine( new object() );
    Console.WriteLine( "{0}", new object() );

    Console.Write( true );
    Console.Write( 'A' );
    Console.Write( "" );
    Console.Write( 1 );
    Console.Write( 1L );
    Console.Write( new object() );
    Console.Write( "{0}", new object() );

    Console.Out.WriteLine();
    Console.Out.WriteLine( true );
    Console.Out.WriteLine( 'A' );
    Console.Out.WriteLine( "" );
    Console.Out.WriteLine( 1 );
    Console.Out.WriteLine( 1L );
    Console.Out.WriteLine( new object() );
    Console.Out.WriteLine( "{0}", new object() );
    Console.Error.WriteLine();
    Console.Error.WriteLine( true );
    Console.Error.WriteLine( 'A' );
    Console.Error.WriteLine( "" );
    Console.Error.WriteLine( 1 );
    Console.Error.WriteLine( 1L );
    Console.Error.WriteLine( new object() );
    Console.Error.WriteLine( "{0}", new object() );

    var _1 = Console.Out;
    var _2 = Console.Error;
  }

  // Remove suppression to verify that all these APIs indeed produce build warnings
  [SuppressMessage( "ApiDesign", "RS0030:Do not use banned APIs" )]
  private static void BannedInAllProjects() {
  }
}