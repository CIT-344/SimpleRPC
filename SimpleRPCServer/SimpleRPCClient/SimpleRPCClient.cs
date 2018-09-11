using System;
using System.Net.Sockets;
using System.Text;

namespace RPCClient
{
    public class SimpleRPCClient
    {
        public readonly Socket _Connection;

        public SimpleRPCClient(String Host, int Port)
        {
            _Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _Connection.Connect(Host, Port);
        }

        public void Send(String text)
        {
            _Connection.Send(Encoding.UTF8.GetBytes(text));
        }
    }
}
