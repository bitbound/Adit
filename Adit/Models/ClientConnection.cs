using Adit.Code.Server;
using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class ClientConnection : IDisposable
    {
        public ServerSocketMessages SocketMessageHandler { get; set; }

        public string ID { get; set; } = Guid.NewGuid().ToString();

        public ConnectionTypes ConnectionType { get; set; }

        public Socket Socket { get; set; }

        public void Dispose()
        {
            SocketMessageHandler = null;
            if (Socket?.Connected == true)
            {
                Socket.Close();
            }
            if (Socket != null)
            {
                Socket.Dispose();
            }
        }

        public void SendJSON(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            var outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            Socket.SendAsync(socketArgs);
        }
        public void SendBytes(byte[] bytes)
        {
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(bytes, 0, bytes.Length);
            Socket.SendAsync(socketArgs);
        }
    }
}
