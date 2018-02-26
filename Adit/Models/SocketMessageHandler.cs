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
        public int ExpectedBinarySize { get; set; }
        public string LastRequesterID { get; set; }
        public Encryption Encryption { get; set; }
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
                SendBytes(bytes);
            }
        }
        public void SendJSONUnencrypted(dynamic jsonData)
        {
            if (socketOut.Connected)
            {
                string jsonRequest = Utilities.JSON.Serialize(jsonData);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonRequest);
                SendBytesUnencrypted(bytes);
            }
        }
        public void SendBytesUnencrypted(byte[] bytes)
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

        public void SendBytes(byte[] bytes)
        {
            if (socketOut.Connected)
            {
                Task.Run(async () => {
                    byte[] outBuffer = bytes;
                    if (Encryption != null)
                    {
                        outBuffer = await Encryption.EncryptBytes(bytes);
                    }
                    var socketArgs = SocketArgsPool.GetSendArg();
                    socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
                    outBuffer.CopyTo(socketArgs.Buffer, 0);
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


        public async void ProcessSocketMessage(SocketAsyncEventArgs socketArgs)
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
                var receivedBytes = socketArgs.Buffer.Take(socketArgs.BytesTransferred).ToArray();
                if (Encryption != null)
                {
                    receivedBytes = await Encryption.DecryptBytes(receivedBytes, true);
                    if (receivedBytes == null)
                    {
                        return;
                    }
                }
                if (Utilities.IsJSONData(receivedBytes))
                {
                    var decodedString = Encoding.UTF8.GetString(receivedBytes);
                    var messages = Utilities.SplitJSON(decodedString);
                    foreach (var message in messages)
                    {
                        ProcessJSONString(message);
                    }
                }
                else
                {
                     this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                            FirstOrDefault(mi => mi.Name == "ReceiveByteArray").Invoke(this, new object[] { receivedBytes });
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }
        private void ProcessJSONString(string message)
        {
            try
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
            catch (Exception ex)
            {
                Utilities.WriteToLog($"Failed to process JSON: {message}");
                Utilities.WriteToLog(ex);
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
