using System.Net.Sockets;

namespace barmud
{
    public enum PlayerStatus {
        Name,
        Password,
        Customization,
        New,
        NewPassword,
        Ready,
    }
    public class MUDSocket
    {
        public Socket Sock;
        public string Name = "default";
        public uint Drunk = 0;
        public long Money = 0;
        public PlayerStatus Status = PlayerStatus.Name;
        public bool LoggedIn = false;

        public MUDSocket(Socket sock) {
            Sock = sock;
        }
    }
}