using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

// ReSharper disable AllUnderscoreLocalParameterName
// ReSharper disable UnusedMember.Local

internal partial class NukeBuild {
  private static AbsolutePath OuiGeneratedFile =>
    RootDirectory / "src" / "Scanning" / "Oui" / "OuiDatabase.Generated.cs";

  private const string OuiCsvUrl = "https://standards-oui.ieee.org/oui/oui.csv";

  /// <summary>
  /// Downloads the latest IEEE OUI CSV and regenerates OuiDatabase.Generated.cs.
  /// </summary>
  Target UpdateOui => _ => _
    .Executes( async () => {
        Log.Information( "Downloading IEEE OUI database from {Url}...", OuiCsvUrl );

        using var http = new HttpClient();
        var csv = await http.GetStringAsync( OuiCsvUrl );

        Log.Information( "Parsing OUI entries..." );

        var entries = ParseOuiCsv( csv );

        Log.Information( "Parsed {Count} vendor entries", entries.Count );

        var source = GenerateSource( entries );

        await File.WriteAllTextAsync( OuiGeneratedFile, source,
          new UTF8Encoding( encoderShouldEmitUTF8Identifier: false ) );

        Log.Information( "Written {Path}", OuiGeneratedFile );
      }
    );

  private sealed class OuiRecord {
    public string Registry {
      get;
      init;
    } = "";

    public string Assignment {
      get;
      init;
    } = "";

    [Name( "Organization Name" )]
    public string OrganizationName {
      get;
      init;
    } = "";
  }

  private static List<(uint, string)> ParseOuiCsv( string csv ) {
    var config = new CsvConfiguration( CultureInfo.InvariantCulture ) { HasHeaderRecord = true, };

    using var reader = new StringReader( csv );
    using var csvReader = new CsvReader( reader, config );

    var entries = new List<(uint, string)>();
    var seen = new HashSet<uint>();

    foreach ( var record in csvReader.GetRecords<OuiRecord>() ) {
      // Only MA-L are standard 24-bit OUI assignments
      if ( record.Registry != "MA-L" ) {
        continue;
      }

      if ( record.Assignment.Length != 6 ) {
        continue;
      }

      var orgName = record.OrganizationName.Trim();
      if ( string.IsNullOrEmpty( orgName ) || orgName == "Private" ) {
        continue;
      }

      if ( !TryParseHex( record.Assignment, out var oui ) ) {
        continue;
      }

      // Deduplicate: keep first occurrence of each OUI
      if ( seen.Add( oui ) ) {
        entries.Add( ( oui, orgName ) );
      }
    }

    entries.Sort( ( a, b ) => a.Item1.CompareTo( b.Item1 ) );
    return entries;
  }

  private static bool TryParseHex( string hex, out uint result ) {
    result = 0;
    try {
      result = Convert.ToUInt32( hex, 16 );
      return true;
    }
    catch {
      return false;
    }
  }

  private static string GenerateSource( List<(uint, string)> entries ) {
    var sb = new StringBuilder();
    sb.AppendLine( "// Auto-generated from the IEEE OUI database." );
    sb.AppendLine( "// Source: https://standards-oui.ieee.org/oui/oui.csv" );
    sb.AppendLine( $"// Total entries: {entries.Count}" );
    sb.AppendLine( "// To refresh: nuke UpdateOui" );
    sb.AppendLine();
    sb.AppendLine( "namespace Drift.Scanning.Oui;" );
    sb.AppendLine();
    sb.AppendLine( "public static partial class OuiDatabase {" );
    sb.AppendLine( $"  private static readonly Dictionary<uint, string> _vendors = new({entries.Count}) {{" );

    foreach ( var entry in entries ) {
      var oui = entry.Item1;
      var vendor = entry.Item2;
      var escaped = vendor.Replace( "\\", "\\\\" ).Replace( "\"", "\\\"" );
      sb.AppendLine( $"    {{ 0x{oui:X6}u, \"{escaped}\" }}," );
    }

    sb.AppendLine( "  };" );
    sb.AppendLine( "}" );

    return sb.ToString();
  }
}