using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class ClientConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public ClientTypes ClientType { get; set; }

        public Socket Socket { get; set; }

        public void Send(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            Socket.SendAsync(socketArgs);
        }

        public enum ClientTypes
        {
            Client,
            Service,
            Viewer
        }
    }
}
