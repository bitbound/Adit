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
        public string LastRequesterID { get; set; }
        public Encryption Encryption { get; set; }
        private List<byte> AggregateMessages { get; set; } = new List<byte>();
        public SocketMessageHandler(Socket socketOut)
        {
            this.socketOut = socketOut;
        }
        public bool IsConnected
        {
            get
            {
                return socketOut?.Connected == true;
            }
        }
        public void SendJSON(dynamic jsonData)
        {
            if (socketOut.Connected)
            {
                string jsonRequest = Utilities.JSON.Serialize(jsonData);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonRequest);
                var messageHeader = new byte[1];
                SendBytes(messageHeader.Concat(bytes).ToArray());
            }
        }
        public void SendJSONUnencrypted(dynamic jsonData)
        {
            if (socketOut.Connected)
            {
                string jsonRequest = Utilities.JSON.Serialize(jsonData);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonRequest);
                var messageHeader = new byte[1];
                SendBytesUnencrypted(messageHeader.Concat(bytes).ToArray());
            }
        }
        public void SendBytesUnencrypted(byte[] bytes)
        {
            if (socketOut.Connected)
            {
                Task.Run(() => {
                    bytes = bytes.Concat(new byte[] { 88,88,88 }).ToArray();
                    var socketArgs = SocketArgsPool.GetSendArg();
                    socketArgs.SetBuffer(bytes, 0, bytes.Length);
                    bytes.CopyTo(socketArgs.Buffer, 0);
                    socketOut.SendAsync(socketArgs);
                });
            }
        }

        public void SendBytes(byte[] bytes)
        {
            if (socketOut.Connected)
            {
                Task.Run(() => {
                    if (Encryption != null)
                    {
                        bytes = Encryption.EncryptBytes(bytes);
                    }
                    bytes = bytes.Concat(new byte[] { 88,88,88 }).ToArray();
                    var socketArgs = SocketArgsPool.GetSendArg();
                    socketArgs.SetBuffer(bytes, 0, bytes.Length);
                    bytes.CopyTo(socketArgs.Buffer, 0);
                    socketOut.SendAsync(socketArgs);
                });
            }
        }
        public void SendRawBytes(byte[] bytes)
        {
            if (socketOut.Connected)
            {
                Task.Run(() => {
                    var socketArgs = SocketArgsPool.GetSendArg();
                    socketArgs.SetBuffer(bytes, 0, bytes.Length);
                    bytes.CopyTo(socketArgs.Buffer, 0);
                    socketOut.SendAsync(socketArgs);
                });
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


        public void ProcessSocketArgs(SocketAsyncEventArgs socketArgs, EventHandler<SocketAsyncEventArgs> completedHandler, Socket socket)
        {
            try
            {
                if (socketArgs.BytesTransferred == 0)
                {
                    if (Config.Current.StartupMode == Config.StartupModes.Notifier)
                    {
                        App.Current.Dispatcher.Invoke(() => {
                            App.Current.Shutdown();
                        });
                    }
                    return;
                }

                if (AggregateMessages.Count == 0 && socketArgs.Buffer.Skip(socketArgs.BytesTransferred - 3).Take(3).All(x => x == 88))
                {
                    ProcessMessage(socketArgs.Buffer.Take(socketArgs.BytesTransferred - 3).ToArray());
                    return;
                }
                else
                {
                    AggregateMessages.AddRange(socketArgs.Buffer.Take(socketArgs.BytesTransferred));
                    if (AggregateMessages.Skip(AggregateMessages.Count - 3).Take(3).All(x => x == 88))
                    {
                        ProcessMessage(AggregateMessages.Take(AggregateMessages.Count - 3).ToArray());
                        AggregateMessages.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                AggregateMessages.Clear();
                
            }
            finally
            {
                if (socket.Connected)
                {
                    if (!socket.ReceiveAsync(socketArgs))
                    {
                        completedHandler(socket, socketArgs);
                    }
                }
            }
        }
        private void ProcessMessage(byte[] messageBytes)
        {
            if (Encryption != null)
            {
                messageBytes = Encryption.DecryptBytes(messageBytes);
                if (messageBytes == null)
                {
                    return;
                }
            }

            if (messageBytes[0] == 0)
            {
                var decodedString = Encoding.UTF8.GetString(messageBytes.Skip(1).ToArray());
                var messages = Utilities.SplitJSON(decodedString);
                foreach (var message in messages)
                {
                    ProcessJSONString(message);
                }
            }
            else
            {
                this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                       FirstOrDefault(mi => mi.Name == "ReceiveByteArray").Invoke(this, new object[] { messageBytes.ToArray() });
            }
            return;
        }

        private void ProcessJSONString(string message)
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
                PassDataToPartner(jsonData);
            }
        }

        private void PassDataToPartner(dynamic jsonData)
        {
            if (this.GetType() == typeof(ServerSocketMessages))
            {
                var partners = (this as ServerSocketMessages)?.Session?.ConnectedClients?.Where(
                     x => x.ID != (this as ServerSocketMessages).ConnectionToClient.ID);
                if (partners != null)
                {
                    foreach (var partner in partners)
                    {
                        partner.SendJSON(jsonData);
                    }
                }
            }
        }
    }
}
