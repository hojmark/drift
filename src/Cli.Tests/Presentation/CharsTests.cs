using Drift.Cli.Presentation.Rendering;

namespace Drift.Cli.Tests.Presentation;

internal class CharsTests {
  [Test]
  public async Task RenderAsEmojisTest() {
    var all = string.Join( "\n", Chars.All() );
    await Verify( all );
  }
}