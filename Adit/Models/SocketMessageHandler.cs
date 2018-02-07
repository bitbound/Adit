using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class SocketMessageHandler
    {
        Socket socketOut;
        public SocketMessageHandler(Socket socketOut)
        {
            this.socketOut = socketOut;
        }
        public void Send(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            socketOut.SendAsync(socketArgs);
        }

        public void SendConnectionType(ConnectionTypes connectionType)
        {
            Send(new
            {
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
            if (Utilities.IsjsonData(trimmedBuffer))
            {
                var decodedString = Encoding.UTF8.GetString(trimmedBuffer);
                var jsonData = Utilities.JSON.Deserialize<dynamic>(decodedString);
                var methodHandler = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                    FirstOrDefault(mi => mi.Name == "Receive" + jsonData["Type"]);
                if (methodHandler != null)
                {
                    try
                    {
                        methodHandler.Invoke(this, new object[] { jsonData });
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
    }
}
