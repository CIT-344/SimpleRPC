using RPCServer;
using System;
using System.Net;

namespace ClientTest
{
    class Program
    {

        static bool Running = true;
        static SimpleRPCClient Client = new SimpleRPCClient(IPAddress.Loopback.ToString(), 11000);
        static void Main(string[] args)
        {
            Console.WriteLine("Press any esc to exit");
            Console.WriteLine("Starting Client");
            
            
            Client.On<DateTime>("SysTime", (date) =>
            {
                Console.Title = ($"Server Time is {date.ToShortTimeString()}");
            });

            Client.On<String>("InMsg", (msg) => 
            {
                Console.WriteLine($"Received Message: {msg}");
            });


            Client.Connect();

            Client.Invoke("SysTime", new DateTime());

            do
            {
                Console.WriteLine("Enter Message to Broadcast: ");
                var msg = Console.ReadLine();

                if (msg.Trim() == "/exit")
                {
                    Client.Disconnect();
                    Running = false;
                }
                else
                {
                    Client.Invoke("OutMsg", msg);
                }
            } while (Running);

            
            
        }
    }
}
