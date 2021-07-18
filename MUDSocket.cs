using System.Net.Sockets;

namespace barmud
{
    public class MUDSocket
    {
        public Socket Sock;
        public string Name = "default";
        public uint Drunk = 0;
        public long Money = 0;

        public MUDSocket(Socket sock) {
            Sock = sock;
        }
    }
}