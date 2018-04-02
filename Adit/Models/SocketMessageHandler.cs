using Adit.Code.Client;
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
        private int ExpectedBinarySize { get; set; }
        private MethodInfo ByteArrayHandler { get; set; }
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
                SendBytes(messageHeader.Concat(bytes).ToArray(), String.Empty, String.Empty);
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
                var messageHeader = new byte[]
                    {
                        (byte)(bytes.Length % 10000000000 / 100000000),
                        (byte)(bytes.Length % 100000000 / 1000000),
                        (byte)(bytes.Length % 1000000 / 10000),
                        (byte)(bytes.Length % 10000 / 100),
                        (byte)(bytes.Length % 100),
                    };

                bytes = messageHeader.Concat(bytes).ToArray();
                var socketArgs = SocketArgsPool.GetSendArg();
                socketArgs.SetBuffer(bytes, 0, bytes.Length);
                socketOut.SendAsync(socketArgs);
            }
        }

        public void SendBytes(byte[] bytes, string recipientID, string senderID)
        {
            if (socketOut.Connected)
            {
                var totalPendingBufferOut = SocketArgsPool.SocketSendArgs
                    .Where(x => x.RecipientID == recipientID)
                    .Sum(x => x.Buffer.Length);
                if (totalPendingBufferOut > Config.Current.BufferSize)
                {
                    var pauseFor = totalPendingBufferOut / Config.Current.BufferSize * 50;
                    if (this is ClientSocketMessages)
                    {
                        var captureInstance = AditClient.ParticipantList.Find(x => x.ID == recipientID)?.CaptureInstance;
                        if (captureInstance != null)
                        {
                            captureInstance.PauseForMilliseconds = pauseFor;
                        }
                    }
                    else if (this is ServerSocketMessages)
                    {
                        var sender = AditServer.ClientList.Find(x => x.ID == senderID);
                        if (sender.ConnectionType == ConnectionTypes.Client || sender.ConnectionType == ConnectionTypes.ElevatedClient)
                        {
                            sender.SocketMessageHandler.SendSlowDown(recipientID, pauseFor);
                        }
                    }
                }
                if (Encryption != null)
                {
                    bytes = Encryption.EncryptBytes(bytes);
                }
                var messageHeader = new byte[]
                {
                        (byte)(bytes.Length % 10000000000 / 100000000),
                        (byte)(bytes.Length % 100000000 / 1000000),
                        (byte)(bytes.Length % 1000000 / 10000),
                        (byte)(bytes.Length % 10000 / 100),
                        (byte)(bytes.Length % 100),
                };

                bytes = messageHeader.Concat(bytes).ToArray();
                var socketArgs = SocketArgsPool.GetSendArg();
                socketArgs.RecipientID = recipientID;
                socketArgs.SetBuffer(bytes, 0, bytes.Length);
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


        public void ProcessSocketArgs(SocketAsyncEventArgs socketArgs, EventHandler<SocketAsyncEventArgs> completedHandler, Socket socket)
        {
            try
            {
                if (socketArgs.BytesTransferred == 0)
                {
                    if (Config.Current.StartupMode == Config.StartupModes.Notifier)
                    {
                        Environment.Exit(0);
                    }
                    return;
                }

                var messageHeader = socketArgs.Buffer[0] * 100000000
                    + socketArgs.Buffer[1] * 1000000
                    + socketArgs.Buffer[2] * 10000
                    + socketArgs.Buffer[3] * 100
                    + socketArgs.Buffer[4];

                if (AggregateMessages.Count == 0 && socketArgs.BytesTransferred - 5 == messageHeader)
                {
                    ProcessMessage(socketArgs.Buffer.Skip(5).Take(socketArgs.BytesTransferred - 5).ToArray());
                    return;
                }
                else
                {
                    if (ExpectedBinarySize == 0)
                    {
                        ExpectedBinarySize = messageHeader;
                    }
                    AggregateMessages.AddRange(socketArgs.Buffer.Take(socketArgs.BytesTransferred));
                    while (AggregateMessages.Count - 5 >= ExpectedBinarySize)
                    {
                        ProcessMessage(AggregateMessages.Skip(5).Take(ExpectedBinarySize).ToArray());
                        AggregateMessages.RemoveRange(0, ExpectedBinarySize + 5);
                        if (AggregateMessages.Count > 0)
                        {
                            ExpectedBinarySize = AggregateMessages[0] * 100000000
                                + AggregateMessages[1] * 1000000
                                + AggregateMessages[2] * 10000
                                + AggregateMessages[3] * 100
                                + AggregateMessages[4];
                        }
                        else
                        {
                            ExpectedBinarySize = 0;
                            AggregateMessages.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                AggregateMessages.Clear();
                ExpectedBinarySize = 0;

            }
            finally
            {
                if (socket != null && socket.Connected)
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
                if (ByteArrayHandler == null)
                {
                    ByteArrayHandler = this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                                            FirstOrDefault(mi => mi.Name == "ReceiveByteArray");
                }
                ByteArrayHandler.Invoke(this, new object[] { messageBytes.ToArray() });
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
            if (this is ServerSocketMessages)
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
