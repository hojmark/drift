using System.Reflection;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using NUnit.Framework.Internal.Commands;

namespace Drift.FeatureFlagsDELETE.Tests;

[AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
public class FeatureFlagMatrixAttribute : NUnitAttribute, ITestBuilder, IWrapTestMethod {
  private readonly NUnitTestCaseBuilder _builder = new();

  // -----------------------
  // ITestBuilder: generates all test cases
  // -----------------------
  public IEnumerable<TestMethod> BuildFrom( IMethodInfo method, Test? suite ) {
    var sources = method.GetCustomAttributes<TestCaseSourceAttribute>( true );

    if ( sources.Any() ) {
      // Cross-product of each TestCaseSource with feature flags
      foreach ( var sourceAttr in sources ) {
        var sourceData = GetTestCaseSourceData( method, sourceAttr );

        foreach ( var caseData in sourceData ) {
          foreach ( var flags in FeatureFlagService.GetAllCombinations( includeEmpty: false ) ) {
            var originalArgs = ExtractArgumentsFromCaseData( caseData );
            var parms = new TestCaseParameters( originalArgs );
            parms.Properties.Set( "FeatureFlags", flags );

            // Add flag info to test name
            parms.TestName = caseData is TestCaseData tcd && tcd.TestName != null
              ? $"{tcd.TestName} [{string.Join( ",", flags )}]"
              : $"{method.Name} [{string.Join( ",", flags )}]";

            yield return _builder.BuildTestMethod( method, suite, parms );
          }
        }
      }
    }
    else {
      // No TestCaseSource: one test per feature flag combination
      foreach ( var flags in FeatureFlagService.GetAllCombinations( includeEmpty: false ) ) {
        var parms = new TestCaseParameters( new object[] { flags } );
        parms.Properties.Set( "FeatureFlags", flags );
        parms.TestName = $"{method.Name} [{string.Join( ",", flags )}]";

        yield return _builder.BuildTestMethod( method, suite, parms );
      }
    }
  }

  // -----------------------
  // IWrapTestMethod: ensures flags are accessed
  // -----------------------
  public TestCommand Wrap( TestCommand command ) {
    return new FeatureFlagCheckCommand( command );
  }

  private class FeatureFlagCheckCommand : DelegatingTestCommand {
    public FeatureFlagCheckCommand( TestCommand inner ) : base( inner ) {
    }

    public override TestResult Execute( TestExecutionContext context ) {
      bool accessedFlags = false;

      // Provide a way for the test to access feature flags
      context.CurrentTest.Properties.Set( "GetFeatureFlags", new Func<HashSet<FeatureFlag>>( () => {
        accessedFlags = true;
        return (HashSet<FeatureFlag>) context.CurrentTest.Properties.Get( "FeatureFlags" )!;
      } ) );

      var result = innerCommand.Execute( context );

      // Fail the test if the flags were never accessed
      if ( !accessedFlags ) {
        result.SetResult( ResultState.Failure, "FeatureFlags were never accessed." );
      }

      return result;
    }
  }

  // -----------------------
  // Helpers
  // -----------------------
  private static object[] ExtractArgumentsFromCaseData( object caseData ) {
    return caseData switch {
      TestCaseData tcd => tcd.Arguments,
      object[] arr => arr,
      _ => new object[] { caseData }
    };
  }

  private static IEnumerable<object> GetTestCaseSourceData( IMethodInfo method, TestCaseSourceAttribute attr ) {
    MemberInfo? sourceMember = attr.SourceType != null
      ? attr.SourceType.GetMember( attr.SourceName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
        .FirstOrDefault()
      : method.TypeInfo.Type
        .GetMember( attr.SourceName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic )
        .FirstOrDefault();

    if ( sourceMember == null )
      throw new InvalidOperationException(
        $"Cannot find member {attr.SourceName} on type {( attr.SourceType ?? method.TypeInfo.Type ).FullName}" );

    return sourceMember switch {
      MethodInfo mi => mi.Invoke( null, null ) as IEnumerable<object> ?? Enumerable.Empty<object>(),
      PropertyInfo pi => pi.GetValue( null ) as IEnumerable<object> ?? Enumerable.Empty<object>(),
      FieldInfo fi => fi.GetValue( null ) as IEnumerable<object> ?? Enumerable.Empty<object>(),
      _ => throw new InvalidOperationException( $"Unsupported member type for {attr.SourceName}" )
    };
  }
}