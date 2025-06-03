using System.Xml.Serialization;
using NmapXmlParser;

namespace Drift.Parsers.NmapXml;

public static class NmapXmlReader {
  //  https://www.nuget.org/packages/NmapXmlParser/
  public static nmaprun Deserialize( string filePath ) {
    using var xmlStream = new StreamReader( filePath );
    var xmlSerializer = new XmlSerializer( typeof(nmaprun) );
    return xmlSerializer.Deserialize( xmlStream ) as nmaprun;
    //try to throw exception here, looks like it's swalloed
  }

  public static nmaprun Deserialize( Stream stream ) {
    using var xmlStream = new StreamReader( stream );
    var xmlSerializer = new XmlSerializer( typeof(nmaprun) );
    return xmlSerializer.Deserialize( xmlStream ) as nmaprun;
  }
}