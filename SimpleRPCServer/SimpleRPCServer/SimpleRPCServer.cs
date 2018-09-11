using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace RPCServer
{
   
    public class SimpleRPCServer
    {
        public readonly IPEndPoint _IPEndpoint;

        private Socket _Socket;

        private Task ServerThread;

        CancellationTokenSource _TokenSource = new CancellationTokenSource();

        Dictionary<Socket, Task> _Clients = new Dictionary<Socket, Task>();

        public SimpleRPCServer(IPAddress Address, int Port)
        {
            _IPEndpoint = new IPEndPoint(Address, Port);
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _Socket.Bind(_IPEndpoint);
        }

        public void SoftStopServer()
        {
            _TokenSource.Cancel();
            ServerThread.Wait();
            Task.WaitAll(_Clients.Select(x=>x.Value).ToArray());
        }

        public void StartServer()
        {
            ServerThread = new Task(()=> 
            {
                _Socket.Listen(100);
                // Enter a loop waiting for connection requests
                // Or because I am RDM I don't need connections
                while (!_TokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var newClient = _Socket.Accept();

                        _Clients.Add(newClient, ClientThread(newClient));
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Canceled");
                    }
                }


                foreach (var client in _Clients)
                {
                    client.Key.Disconnect(false);
                }
                
            },_TokenSource.Token, TaskCreationOptions.LongRunning);

            ServerThread.Start();
        }


        private Task ClientThread(Socket MyClient)
        {
            return Task.Factory.StartNew(()=>
            {
                _TokenSource.Token.
                while (!_TokenSource.IsCancellationRequested)
                {
                    var buffer = new byte[1024];
                    var bRecieved = MyClient.Receive(buffer, 1024, SocketFlags.Partial);
                    var result = Encoding.UTF8.GetString(buffer.Take(bRecieved).ToArray());
                }
                
            },_TokenSource.Token);
        }
    }
}
