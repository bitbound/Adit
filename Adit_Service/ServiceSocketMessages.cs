using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Win32_Classes;

namespace Adit_Service
{
    class ServiceSocketMessages
    {
        Socket socketOut;
        public ServiceSocketMessages(Socket socketOut)
        {
            this.socketOut = socketOut;
        }
        public Encryption Encryptor { get; set; }
        private List<byte> AggregateMessages { get; set; } = new List<byte>();
        private int ExpectedBinarySize { get; set; }
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
        public void SendBytes(byte[] bytes)
        {
            if (socketOut.Connected)
            {
                if (Encryptor != null)
                {
                    bytes = Encryptor.EncryptBytes(bytes);
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
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
            finally
            {
                if (!socket.ReceiveAsync(socketArgs))
                {
                    completedHandler(socket, socketArgs);
                }
            }

        }

        private void ProcessMessage(byte[] messageBytes)
        {
            if (Encryptor != null)
            {
                messageBytes = Encryptor.DecryptBytes(messageBytes);
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
        }
        private void ReceiveRequestForElevatedClient(dynamic jsonData)
        {
            var sessionID = Guid.NewGuid().ToString();
            var desktopName = User32.GetCurrentDesktop();
            var procInfo = new ADVAPI32.PROCESS_INFORMATION();
            var processResult = ADVAPI32.OpenInteractiveProcess(Path.Combine(Utilities.ProgramFolder, "Adit.exe") + $" -upgrade {sessionID}", desktopName, out procInfo);
            if (processResult == false)
            {
                jsonData["Status"] = "failed";
                SendJSON(jsonData);
                Utilities.WriteToLog(new Exception("Error opening interactive process.  Error Code: " + Marshal.GetLastWin32Error().ToString()));
            }
            else
            {
                jsonData["Status"] = "ok";
                jsonData["ClientSessionID"] = sessionID;
                SendJSON(jsonData);
            }
        }

        private void ReceiveEncryptionStatus(dynamic jsonData)
        {
            try
            {
                if (jsonData["Status"] == "On")
                {
                    Encryptor = new Encryption();
                    Encryptor.Key = Encryption.GetStoredKey();
                }
                else if (jsonData["Status"] == "Failed")
                {
                    throw new Exception("Server failed to start encrypted connection.");
                }
                SendConnectionType(ConnectionTypes.Service);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                Environment.Exit(0);
            }
        }
        private void ReceiveSAS(dynamic jsonData)
        {
            User32.SendSAS(false);
        }
        public void SendHeartbeat()
        {
            if (!AditService.HeartbeatTimer.Enabled)
            {
                AditService.HeartbeatTimer.Elapsed += (sender, args) =>
                {
                    SendHeartbeat();
                };
                AditService.HeartbeatTimer.Interval = 30000;
                AditService.HeartbeatTimer.Start();
            }
            try
            {
                if (!AditService.IsConnected)
                {
                    AditService.HeartbeatTimer.Stop();
                    AditService.WaitToRetryConnection();
                    return;
                }
                var uptime = new PerformanceCounter("System", "System Up Time", true);
                var macAddress = Utilities.GetMACAddress();
                uptime.NextValue();
                string currentUser;
                try
                {
                    var mos = new ManagementObjectSearcher("Select * FROM Win32_Process WHERE ExecutablePath LIKE '%explorer.exe%'");
                    var col = mos.Get();
                    var process = col.Cast<ManagementObject>().First();
                    var ownerInfo = new string[2];
                    process.InvokeMethod("GetOwner", ownerInfo);
                    currentUser = ownerInfo[1] + "\\" + ownerInfo[0];
                }
                catch
                {
                    currentUser = "";
                }

                var request = new
                {
                    Type = "Heartbeat",
                    ComputerName = Environment.MachineName,
                    CurrentUser = currentUser,
                    LastReboot = (DateTime.Now - TimeSpan.FromSeconds(uptime.NextValue())),
                    MACAddress = macAddress
                };
                SendJSON(request);
                Utilities.CleanupFiles();
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
            }
        }

    }
}
