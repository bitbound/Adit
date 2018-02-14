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
            if (socketOut.Connected)
            {
                string jsonRequest = Utilities.JSON.Serialize(jsonData);
                byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
                var socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
                socketArgs.Completed += (sender, args) => {
                    socketArgs.Dispose();
                };
                socketOut.SendAsync(socketArgs);
            }
        }
        public void SendBytes(byte[] bytes)
        {
            if (socketOut.Connected)
            {
                var socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(bytes, 0, bytes.Length);
                socketArgs.Completed += (sender, args) => {
                    socketArgs.Dispose();
                };
                socketOut.SendAsync(socketArgs);
            }
        }

        public void SendConnectionType(ConnectionTypes connectionType)
        {
            SendJSON(new
            {
                Type = "ConnectionType",
                ConnectionType = connectionType.ToString()
            });
        }

        public void ProcessSocketMessage(SocketAsyncEventArgs socketArgs)
        {
            if (socketArgs.BytesTransferred == 0)
            {
                return;
            }
            var trimmedBuffer = socketArgs.Buffer.Take(socketArgs.BytesTransferred).ToArray();
            if (Utilities.IsJSONData(trimmedBuffer))
            {
                var decodedString = Encoding.UTF8.GetString(trimmedBuffer);
                var messages = Utilities.SplitJSON(decodedString);
                foreach (var message in messages)
                {
                    var jsonData = Utilities.JSON.Deserialize<dynamic>(message);
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
                        }
                    }
                    else
                    {
                        if (this.GetType() == typeof(ServerSocketMessages))
                        {
                            var partners = (this as ServerSocketMessages).Session.ConnectedClients.Where(
                                x => x.ConnectionType != (this as ServerSocketMessages).ConnectionToClient.ConnectionType);
                            foreach (var partner in partners)
                            {
                                partner.SendJSON(jsonData);
                            }
                        }
                    }
                }
                
            }
            else
            {
                try
                {
                    this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                        FirstOrDefault(mi => mi.Name == "ReceiveByteArray").Invoke(this, new object[] { trimmedBuffer });
                }
                catch
                {
                    
                }
            }
        }

    }
}
