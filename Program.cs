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
                try
                {
                    server.ProcessMessages();
                    Thread.Sleep(200);
                } catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
