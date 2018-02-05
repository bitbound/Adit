using Adit.Client_Code;
using Adit.Models;
using Adit.Pages;
using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Shared_Code
{
    public class ClientSocketMessages
    {
        Socket socketOut;
        public ClientSocketMessages(Socket socketOut)
        {
            this.socketOut = socketOut;
        }
        private void Send(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            socketOut.SendAsync(socketArgs);
        }

        public void SendConnectionType(ConnectionTypes connectionType)
        {
            Send(new {
                Type = "ConnectionType",
                ConnectionType = connectionType.ToString()
            });
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
                    FirstOrDefault(mi => mi.Name == "Receive" + jsonMessage["Type"]);
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

        private void ReceiveSessionID(dynamic jsonMessage)
        {
            AditClient.SessionID = jsonMessage["SessionID"];
            ClientMain.Current.RefreshUICall();
        }
    }
}
