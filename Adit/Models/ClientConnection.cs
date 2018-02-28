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
    public class ClientConnection
    {
        public ServerSocketMessages SocketMessageHandler { get; set; }

        public string ID { get; set; } = Guid.NewGuid().ToString();

        public string SessionID
        {
            get
            {
                return SocketMessageHandler?.Session?.SessionID;
            }
        }
        public string ComputerName { get; set; }
        public DateTime? LastReboot { get; set; }
        public string Alias { get; set; }
        public string CurrentUser { get; set; }
        public ConnectionTypes ConnectionType { get; set; }
        public string MACAddress { get; set; }

        public Socket Socket { get; set; }

        public void Close()
        {
            SocketMessageHandler = null;
            if (Socket?.Connected == true)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Disconnect(false);
            }
            if (Socket != null)
            {
                Socket.Dispose();
            }
        }

        public void SendJSON(dynamic jsonData)
        {
            SocketMessageHandler.SendJSON(jsonData);
        }

        public void SendBytes(byte[] bytes)
        {
            SocketMessageHandler.SendBytes(bytes);
        }
    }
}
