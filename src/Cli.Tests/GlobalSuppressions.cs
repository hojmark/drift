  // This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Test infrastructure classes are intentionally public to be used across test files
[assembly:
  SuppressMessage( "Design", "CA1515:Consider making public types internal",
    Justification = "Test infrastructure is intentionally public", Scope = "namespaceanddescendants",
    Target = "~N:Drift.Cli.Tests.Utils.Agent" )]
[assembly:
  SuppressMessage( "Design", "CA1515:Consider making public types internal",
    Justification = "Test infrastructure is intentionally public", Scope = "namespaceanddescendants",
    Target = "~N:Drift.Cli.Tests.Utils.Testing" )]
[assembly:
  SuppressMessage( "Design", "CA1515:Consider making public types internal",
    Justification = "Test infrastructure is intentionally public", Scope = "namespaceanddescendants",
    Target = "~N:Drift.Cli.Tests.Utils.Network.Firewall" )]
[assembly:
  SuppressMessage( "Design", "CA1515:Consider making public types internal",
    Justification = "Test infrastructure is intentionally public", Scope = "namespaceanddescendants",
    Target = "~N:Drift.Cli.Tests.Utils.Network.Topology" )]