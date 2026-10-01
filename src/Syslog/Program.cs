using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

const int SyslogPort = 5140;

Console.WriteLine( "=========================================================" );
Console.WriteLine( $"Listening on port {SyslogPort} (UDP)" );
Console.WriteLine( "=========================================================\n" );

using var listener = new UdpClient( SyslogPort );
while ( true ) {
  try {
    var received = await listener.ReceiveAsync();
    string rawMessage = Encoding.UTF8.GetString( received.Buffer );

    ProcessLog( rawMessage );
  }
  catch ( Exception ex ) {
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine( $"Error: {ex.Message}" );
    Console.ResetColor();
  }
}

void ProcessLog( string log ) {
  string action = "??";
  string sourceIp = "Unknown-Source";
  string destinationIp = "Unknown-Destination";
  string destinationPort = "??";
  string protocol = "??";
  string domain = "";

  string logLowerCase = log.ToLower();
  if (
    logLowerCase.Contains( "block" ) ||
    logLowerCase.Contains( "blocked" ) ||
    logLowerCase.Contains( "drop" ) ||
    logLowerCase.Contains( "reject" ) ||
    logLowerCase.Contains( "deny" )
  ) {
    action = "DENIED";
  }
  else if (
    logLowerCase.Contains( "allow" ) ||
    logLowerCase.Contains( "allowed" ) ||
    logLowerCase.Contains( "passed" ) ||
    logLowerCase.Contains( "accept" )
  ) {
    action = "ALLOWED";
  }

  try {
    // Source IP
    var matchSrcClient = Regex.Match( log, @"UNIFIsrcClientIp=([^\s]+)", RegexOptions.IgnoreCase );
    var matchSrc = Regex.Match( log, @"\bsrc=([^\s]+)", RegexOptions.IgnoreCase );

    if ( matchSrcClient.Success ) {
      sourceIp = matchSrcClient.Groups[1].Value;
    }
    else if ( matchSrc.Success ) {
      sourceIp = matchSrc.Groups[1].Value;
    }

    // Destination IP
    var matchDst = Regex.Match( log, @"\bdst=([^\s]+)", RegexOptions.IgnoreCase );
    if ( matchDst.Success ) {
      destinationIp = matchDst.Groups[1].Value;
    }

    // Destination port
    var matchDpt = Regex.Match( log, @"\bdpt=([^\s]+)", RegexOptions.IgnoreCase );
    if ( matchDpt.Success ) {
      destinationPort = matchDpt.Groups[1].Value;
    }

    // Protocol
    var matchProto = Regex.Match( log, @"\bproto=([^\s]+)", RegexOptions.IgnoreCase );
    if ( matchProto.Success ) {
      protocol = matchProto.Groups[1].Value.ToUpper();
    }

    // Domain (if avail)
    var matchDomain = Regex.Match( log, @"UNIFIdstDomain=([^\s]+)", RegexOptions.IgnoreCase );
    if ( matchDomain.Success ) {
      domain = matchDomain.Groups[1].Value;
    }

    // Print result
    string timestamp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss" );
    string destinationDisplay =
      string.IsNullOrEmpty( domain )
        ? $"{destinationIp}:{destinationPort}"
        : $"{domain} ({destinationIp}:{destinationPort})";

    if ( action == "ALLOWED" ) {
      Console.ForegroundColor = ConsoleColor.Green;
      Console.WriteLine( $"[{timestamp}] [ALLOWED] {sourceIp} -> {destinationDisplay} ({protocol})" );
    }
    else if ( action == "DENIED" ) {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine( $"[{timestamp}] [DENIED]  {sourceIp} -> {destinationDisplay} ({protocol})" );
    }
    else {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine( $"[{timestamp}] [RAW LOG] {log.Trim()}" );
    }

    Console.ResetColor();
  }
  catch ( Exception ex ) {
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine(
      $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [PARSE ERROR - RAW]: {log.Trim()} (Error: {ex.Message})" );
    Console.ResetColor();
  }
}