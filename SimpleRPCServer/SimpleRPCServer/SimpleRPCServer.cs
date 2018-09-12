using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RPCServer
{
   
    public class SimpleRPCServer
    {

        private Dictionary<String, ActionWorker> Events = new Dictionary<string, ActionWorker>();

        public void On(String EventName, Action Work)
        {
            On<object>(EventName, new Action<object>((x) =>
            {
                Work();
            }));
        }

        public void On<T>(String EventName, Action<T> Work)
        {
            Events.Add(EventName, new ActionWorker()
            {
                Worker = new Action<object>((x) =>
                {
                    Work((T)Convert.ChangeType(x, typeof(T)));
                }),
                WorkerType = typeof(T)
            });
        }

        public readonly IPEndPoint BindingEndpoint;

        private Socket Connection_Server;

        private Task ServerThread;

        CancellationTokenSource _TokenSource = new CancellationTokenSource();

        List<SimpleRPCClient> Clients = new List<SimpleRPCClient>();

        public SimpleRPCServer(IPAddress Address, int Port)
        {
            BindingEndpoint = new IPEndPoint(Address, Port);
            Connection_Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            Connection_Server.Bind(BindingEndpoint);
            On<String>("Disconnect", (old_client_ip) => 
            {
                DisconnectByIP(old_client_ip);
            });
        }

        public void SoftStopServer()
        {
            Connection_Server.Close();
            _TokenSource.Cancel();
            ServerThread.Wait();
        }

        public void StartServer()
        {
            ServerThread = new Task(()=> 
            {
                Connection_Server.Listen(100);
                // Enter a loop waiting for connection requests
                while (!_TokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var newClient = Connection_Server.Accept();
                        
                        Clients.Add(new SimpleRPCClient(newClient, Events));

                        newClient.Send("GUID", Guid.NewGuid().ToString());
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("Canceled");
                    }
                    catch (SocketException)
                    {
                        // Probably closed
                    }
                }

                DisconnectAllClients();
                
            },_TokenSource.Token, TaskCreationOptions.LongRunning);
            

            ServerThread.Start();
        }

        public void DisconnectByIP(String ip)
        {
            var client = Clients.Where(x => x.Connection.RemoteEndPoint.ToString() == ip).SingleOrDefault();
            if (client != null)
            {
                DisconnectClient(client);
            }
        }

        public void DisconnectClient(SimpleRPCClient Worker)
        {
            DisconnectById(Worker.ClientId);
        }

        public void DisconnectById(String ClientId)
        {
            var client = Clients.Where(x=>x.ClientId == ClientId).SingleOrDefault();
            if (client != null)
            {
                client.Disconnect();
                Clients.Remove(client);
            }
        }

        public void DisconnectAllClients()
        {
            foreach (var client in Clients)
            {
                client.Disconnect();
            }

            Clients.Clear();
        }
        
        public void All(String EventName, object data)
        {
            foreach (var client in Clients)
            {
                client.Connection.Send(EventName, data);
            }
        }

    }


    internal static class MessageHelper
    {
        public static CommunicationModel GetObject(byte[] Data)
        {
            var data = JsonConvert.DeserializeObject<CommunicationModel>(Encoding.UTF8.GetString(Data));
            return data;
        }

        public static String GetJson(String EventName, Object data = null, String ClientId = null)
        {
            return JsonConvert.SerializeObject(new CommunicationModel()
            {
                EventName = EventName,
                Sent = DateTime.Now,
                Body = data,
                ClientId = ClientId
            });
        }
    }

    internal static class SocketHelpers
    {
        
        public static int Send(this Socket Conn, String EventName, Object data, String ClientId = null)
        {
            // 1 - Convert Data to JSON String - Done
            // 2 - Split that into 1024 chunks
            // 3 - Send Chunks over to desired client
            try
            {
                var JSON = MessageHelper.GetJson(EventName, data, ClientId);

                var Buffer = new Queue<byte>();
                Buffer.Enqueue(Encoding.UTF8.GetBytes(JSON));

                do
                {
                    var sendingBuffer = Buffer.Dequeue(1024);
                    if (Buffer.HasDataRemaining())
                    {
                        Conn.Send(sendingBuffer.ToArray(), SocketFlags.Partial);
                    }
                    else
                    {
                        Conn.Send(sendingBuffer.ToArray());
                    }
                } while (Buffer.HasDataRemaining());

            }
            catch (Exception x)
            {

            }

            return 0;
        }

        internal static void Enqueue(this Queue<byte> Q, IEnumerable<byte> Data)
        {
            foreach (var b in Data)
            {
                Q.Enqueue(b);
            }
        }

        internal static IEnumerable<byte> Dequeue(this Queue<byte> Q, int Count)
        {
            var data = new List<byte>();
            for (int i = 0; i < Count && Q.HasDataRemaining(); i++)
            {
                data.Add(Q.Dequeue());
            }
            return data;
        }

        internal static bool HasDataRemaining(this Queue<byte> Q)
        {
            return Q.Count() != 0;
        }
    }
}
