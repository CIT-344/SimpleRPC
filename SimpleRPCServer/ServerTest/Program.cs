using RPCServer;
using System;
using System.Net;

namespace ServerClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server");
            SimpleRPCServer _Server = new SimpleRPCServer(IPAddress.Any, 11000);


            _Server.On<String>("SysTime", (serverTime) =>
            {
                _Server.All("SysTime", DateTime.Now);
            });

            _Server.On<String>("OutMsg", (msg) =>
            {
                _Server.All("InMsg", msg);
            });


            _Server.StartServer();

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();

        }
    }
}
