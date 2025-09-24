using Drift.Domain.Device;
using Drift.Domain.Device.Addresses;

namespace Drift.Cli.Presentation.Rendering;

internal static class DeviceIdHighlighter {
  internal static IdMarkingStyle Style {
    get;
    set;
  } = IdMarkingStyle.Color;

  internal static string Mark( string text, AddressType type, DeviceId? idDeclared ) {
    if ( idDeclared != null && idDeclared.Contributes( type ) ) {
      // return "[bold]" + text + "[/]";
      return Style switch {
        IdMarkingStyle.Color => text,
        IdMarkingStyle.Dot => $"{text} [blue]•[/]", // ◦•
        _ => throw new NotImplementedException()
      };
    }

#pragma warning disable S3923
    return idDeclared == null
      ? $"[gray]{text}[/]" // TODO use yellow?
      : $"[gray]{text}[/]";
#pragma warning restore S3923
  }
}