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
        public BinaryTransferType ReceiveTransferType { get; set; }
        public int ExpectedBinarySize { get; set; }
        public string LastRequesterID { get; set; }
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
                byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
                var socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
                socketArgs.Completed += (sender, args) =>
                {
                    socketArgs.Dispose();
                };
                socketOut.SendAsync(socketArgs);
            }
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
            if (socketOut.Connected)
            {
                var socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(bytes, 0, bytes.Length);
                socketArgs.Completed += (sender, args) =>
                {
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
                var trimmedBuffer = socketArgs.Buffer.Take(socketArgs.BytesTransferred).ToArray();
                var decodedString = Encoding.UTF8.GetString(trimmedBuffer);
                if (Utilities.IsJSONData(trimmedBuffer))
                {
                    var messages = Utilities.SplitJSON(decodedString);
                    foreach (var message in messages)
                    {
                        ProcessJSONString(message);
                    }
                }
                else
                {
                    try
                    {
                        this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                            FirstOrDefault(mi => mi.Name == "ReceiveByteArray").Invoke(this, new object[] { trimmedBuffer });
                    }
                    catch (Exception ex)
                    {
                        Utilities.WriteToLog(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
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
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
    }
}
