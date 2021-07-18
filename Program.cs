using System;
using System.Threading;

namespace barmud
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Server server = new Server(4000);
            server.Listen();
            while (true) {
                server.ProcessMessages();
                Thread.Sleep(200);
            }
        }
    }
}
