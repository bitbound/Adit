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
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            var outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            socketArgs.Completed += (sender, args) => {
                socketArgs.Dispose();
            };
            Socket.SendAsync(socketArgs);
        }
        public void SendBinaryTransferNotification(BinaryTransferType transferType, int binaryLength, dynamic extraData)
        {
            SendJSON(new
            {
                Type = "BinaryTransferStarting",
                TransferType = transferType.ToString(),
                Size = binaryLength,
                ExtraData = extraData
            });
        }
        public void SendBytes(byte[] bytes)
        {
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(bytes, 0, bytes.Length);
            socketArgs.Completed += (sender, args) => {
                socketArgs.Dispose();
            };
            if (Socket.Connected)
            {
                Socket.SendAsync(socketArgs);
            }
        }
    }
}
