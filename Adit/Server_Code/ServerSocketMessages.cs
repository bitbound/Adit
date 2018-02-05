using Adit.Models;
using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Server_Code
{
    public class ServerSocketMessages
    {
        ClientConnection connectionToClient;
        public ServerSocketMessages(ClientConnection connection)
        {
            this.connectionToClient = connection;
        }
        private void Send(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            connectionToClient.Socket.SendAsync(socketArgs);
        }

        public bool ProcessSocketMessage(byte[] buffer)
        {
            var trimmedBuffer = Utilities.TrimBytes(buffer);
            if (trimmedBuffer.Count() == 0)
            {
                return false;
            }
            if (Utilities.IsJSONMessage(trimmedBuffer))
            {
                var jsonMessage = (dynamic)Utilities.JSON.DeserializeObject(Encoding.UTF8.GetString(trimmedBuffer));
                var methodHandler = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                    FirstOrDefault(mi =>mi.Name == "Receive" + jsonMessage["Type"]);
                if (methodHandler != null)
                {
                    try
                    {
                        methodHandler.Invoke(this, new object[] { jsonMessage });
                    }
                    catch (Exception ex)
                    {
                        Utilities.WriteToLog(ex);
                        throw ex;
                    }
                }
            }
            else
            {

            }
            return true;
        }

        private void ReceiveConnectionType(dynamic jsonMessage)
        {
            switch (jsonMessage["ConnectionType"])
            {
                case "Client":
                    connectionToClient.ConnectionType = ConnectionTypes.Client;
                    SendSessionID();
                    break;
                default:
                    break;
            }
        }

        private void SendSessionID()
        {
            var session = AditServer.SessionList.Find(x => x.ConnectedClients.Contains(connectionToClient));
            Send( new { Type = "SessionID", SessionID = session.SessionID });
        }
    }
}
