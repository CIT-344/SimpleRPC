using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPCServer
{
    public class SimpleRPCClient
    {
        private Dictionary<String, ActionWorker> Events = new Dictionary<string, ActionWorker>();
        public String ClientId;

        public readonly String Host;
        public readonly int Port;

        internal readonly Socket Connection;

        private Task CurrentWorker;

        ManualResetEvent connectSignal = new ManualResetEvent(false);

        public SimpleRPCClient(Socket Client, Dictionary<String, ActionWorker> Events)
        {
            Connection = Client;
            this.Events = Events;
            StartListener();
        }

        public SimpleRPCClient(String Host, int Port)
        {
            Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            this.Host = Host;
            this.Port = Port;
            
            On<String>("GUID", (id) =>
            {
                ClientId = id;
                connectSignal.Set();
            });
        }

        public void Disconnect()
        {
            Connection.Disconnect(false);
        }
        
        public void Invoke(String EventName, object data = null)
        {
            Connection.Send(EventName, data, ClientId);
        }

        public void On(String EventName, Action Work)
        {
            On<object>(EventName, new Action<object>((x)=> 
            {
                Work();
            }));
        }

        public void On<T>(String EventName, Action<T> Work)
        {
            Events.Add(EventName, new ActionWorker()
            {
                Worker = new Action<object>((x)=> 
                {
                    Work((T)Convert.ChangeType(x, typeof(T)));
                }),
                WorkerType = typeof(T)
            });
        }

        private void StartListener()
        {
            CurrentWorker = new Task(() =>
            {
                // Wait for recieve events
                while (Connection.Connected)
                {
                    try
                    {
                        // Keep listening and performing actions
                        var buffer = new byte[1024];
                        var result = Connection.Receive(buffer);
                        if (result == 0)
                        {
                            var e = Events.Where(x => x.Key == "Disconnect").Select(x => x.Value.Worker).Single();
                            e.Invoke(Connection.RemoteEndPoint.ToString());
                            throw new Exception("Client Disconnected");
                        }
                        else
                        {
                            var data = MessageHelper.GetObject(buffer);
                            var events = Events.Where(x => x.Key == data.EventName).Select(x => x.Value.Worker);
                            if (events != null && events.Count() != 0)
                            {
                                foreach (var e in events)
                                {
                                    e.Invoke(data.Body);
                                }
                            }
                        }
                    }
                    catch (SocketException socketError)
                    {

                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }, TaskCreationOptions.LongRunning);
            CurrentWorker.Start();
        }

        public void Connect()
        {
            if (CurrentWorker == null)
            {
                Connection.Connect(Host, Port);

                StartListener();

                connectSignal.WaitOne();


            }
        }
    }
}
