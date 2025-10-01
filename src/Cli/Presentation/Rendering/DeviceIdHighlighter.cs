using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Presentation.Rendering;

internal static class DeviceIdHighlighter {
  internal static IdMarkingStyle Style = IdMarkingStyle.Text;

  internal static string Mark( string text, AddressType type, DeviceId? idDeclared ) {
    if ( idDeclared != null && idDeclared.Contributes( type ) ) {
      //return "[bold]" + text + "[/]";
      return Style switch {
        IdMarkingStyle.Text => text,
        IdMarkingStyle.Dot => $"{text} [blue]•[/]", // ◦•
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    return idDeclared == null
      ? $"[gray]{text}[/]" // TODO use yellow?
      : $"[gray]{text}[/]";
  }
}