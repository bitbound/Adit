using Adit.Code.Server;
using Adit.Code.Shared;
using Adit.Code.Viewer;
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
        public void SendJSON(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            socketOut.SendAsync(socketArgs);
        }
        public void SendBytes(byte[] bytes)
        {
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(bytes, 0, bytes.Length);
            socketOut.SendAsync(socketArgs);
        }

        public void SendConnectionType(ConnectionTypes connectionType)
        {
            SendJSON(new
            {
                Type = "ConnectionType",
                ConnectionType = connectionType.ToString()
            });
        }

        public bool ProcessSocketMessage(SocketAsyncEventArgs socketArgs)
        {
            if (socketArgs.BytesTransferred == 0)
            {
                return false;
            }
            var trimmedBuffer = socketArgs.Buffer.Take(socketArgs.BytesTransferred).ToArray();
            if (Utilities.IsJSONData(trimmedBuffer))
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
                        return false;
                    }
                }
            }
            else
            {
                this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                     FirstOrDefault(mi => mi.Name == "ReceiveByteArray").Invoke(this, new object[] { trimmedBuffer });
            }
            return true;
        }
    }
}
