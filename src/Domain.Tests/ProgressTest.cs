using Drift.Domain.NeoProgress;
using Drift.TestUtilities;
using Microsoft.Extensions.Logging;

namespace Drift.Core.Scan.Tests;

public class ProgressTest {
  [Test]
  public async Task Flow() {
    var logger = new StringLogger();

    var progressBuilder = new ProgressBuilderNew( a => logger.LogInformation( a.TotalProgress + "%: {Path}", a.Path ) );

    var root = progressBuilder.Root;

    var step1 = root.Add( "Step 1" );
    var step1_1 = step1.Add( "Step 1.1" );
    var step1_2 = step1.Add( "Step 1.2" );
    var step2 = root.Add( "Step 2" );
    var step3 = root.Add( "Step 3" );

    Assert.That( root.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step1.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step1_1.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step1_2.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step2.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step3.TotalProgress, Is.EqualTo( 0 ) );

    step1_1.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 25 ) );
    Assert.That( step1.TotalProgress, Is.EqualTo( 50 ) );
    Assert.That( step1_1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_2.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step2.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step3.TotalProgress, Is.EqualTo( 0 ) );

    step1_2.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 50 ) );
    Assert.That( step1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_2.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step2.TotalProgress, Is.EqualTo( 0 ) );
    Assert.That( step3.TotalProgress, Is.EqualTo( 0 ) );

    step2.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 75 ) );
    Assert.That( step1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_2.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step2.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step3.TotalProgress, Is.EqualTo( 0 ) );

    step3.Complete();

    Assert.That( root.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_1.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step1_2.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step2.TotalProgress, Is.EqualTo( 100 ) );
    Assert.That( step3.TotalProgress, Is.EqualTo( 100 ) );

    await Verify( logger.ToString() );
  }

  [Test]
  public async Task Complete_ThrowsOnNonLeafNode() {
    var progressBuilder = new ProgressBuilderNew();

    var root = progressBuilder.Root;

    var step1 = root.Add( "Step 1" );
    step1.Add( "Step 1.1" );

    Assert.Throws<InvalidOperationException>( step1.Complete );
  }
}