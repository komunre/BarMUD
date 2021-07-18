using System;
using System.Net.Sockets;

namespace barmud
{
    public class Debugger
    {
        public static void LogClient(Socket socket, string msg) {
            Console.WriteLine("[{0}] {1}", socket.RemoteEndPoint.ToString(), msg);
        }
    }
}