using System.Net;
using System.Net.Sockets;

namespace Drift.TestUtilities.IO;

public static class TcpUtils {
  public static ushort GetFreePort() {
    using var listener = new TcpListener( IPAddress.Loopback, 0 );
    listener.Start();
    return (ushort) ( (IPEndPoint) listener.LocalEndpoint ).Port;
  }
}