using RPCClient;
using RPCServer;
using System;
using System.Net;

namespace SimpleTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server");
            SimpleRPCServer _Server = new SimpleRPCServer(IPAddress.Any, 11000);
            _Server.StartServer();


            SimpleRPCClient _Client = new SimpleRPCClient(IPAddress.Loopback.ToString(), 11000);
            Console.WriteLine("Enter Text to end to Server:");
            var text = Console.ReadLine();
            _Client.Send(text);

            _Server.SoftStopServer();

            Console.ReadKey();

        }
    }
}
