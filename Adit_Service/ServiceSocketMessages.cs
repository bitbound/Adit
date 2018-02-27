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
        public Encryption Encryption { get; set; }
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
                Task.Run(async () => {
                    if (Encryption != null)
                    {
                        bytes = await Encryption.EncryptBytes(bytes);
                    }
                    bytes = bytes.Concat(new byte[] { 44, 44, 44 }).ToArray();
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
                    return;
                }



                var partialMessages = new string[0];

                if (socketArgs.BufferList[0].Skip(socketArgs.BytesTransferred - 3).Take(3).All(x => x == 44))
                {
                    socketArgs.BufferList.Add(
                        new ArraySegment<byte>(socketArgs.BufferList[0].Take(socketArgs.BytesTransferred - 3).ToArray())
                    );
                }
                else
                {
                    var decodedMessageString = Encoding.UTF8.GetString(socketArgs.BufferList[0].Take(socketArgs.BytesTransferred).ToArray());
                    partialMessages = decodedMessageString.Split(new string[] { ",,," }, StringSplitOptions.RemoveEmptyEntries);
                    socketArgs.BufferList.Add(
                        new ArraySegment<byte>(Encoding.UTF8.GetBytes(partialMessages[0]))
                    );
                }



                var totalBytesReceived = new List<byte>();
                for (var i = 1; i < socketArgs.BufferList.Count; i++)
                {
                    totalBytesReceived.AddRange(socketArgs.BufferList[i]);
                }


                ProcessMessage(totalBytesReceived);

                while (socketArgs.BufferList.Count > 1)
                {
                    socketArgs.BufferList.RemoveAt(1);
                }

                if (partialMessages.Length > 0)
                {
                    socketArgs.BufferList.Add(new ArraySegment<byte>(Encoding.UTF8.GetBytes(partialMessages.Last())));
                    if (partialMessages.Length > 2)
                    {
                        for (var i = 1; i < partialMessages.Length - 1; i++)
                        {
                            ProcessMessage(Encoding.UTF8.GetBytes(partialMessages[i]).ToList());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                while (socketArgs.BufferList.Count > 1)
                {
                    socketArgs.BufferList.RemoveAt(1);
                }
            }
            finally
            {
                if (!socket.ReceiveAsync(socketArgs))
                {
                    completedHandler(socket, socketArgs);
                }
            }

        }

        private async void ProcessMessage(List<byte> totalBytesReceived)
        {
            if (Encryption != null)
            {
                var bytesReceivedThisMessage = await Encryption.DecryptBytes(totalBytesReceived.ToArray());
                if (bytesReceivedThisMessage == null)
                {
                    return;
                }
            }

            var expectedSize = totalBytesReceived[1] * 100000000
                  + totalBytesReceived[2] * 1000000
                  + totalBytesReceived[3] * 10000
                  + totalBytesReceived[4] * 100
                  + totalBytesReceived[5];

            if (totalBytesReceived.Count < expectedSize)
            {
                return;
            }
            else if (totalBytesReceived.Count > expectedSize)
            {
                Utilities.WriteToLog("Total bytes received exceeded expected size.");
            }

            if (Utilities.IsJSONData(totalBytesReceived.Skip(6).ToArray()))
            {
                var decodedString = Encoding.UTF8.GetString(totalBytesReceived.Skip(6).ToArray());
                var messages = Utilities.SplitJSON(decodedString);
                foreach (var message in messages)
                {
                    ProcessJSONString(message);
                }
            }
            else
            {
                this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).
                       FirstOrDefault(mi => mi.Name == "ReceiveByteArray").Invoke(this, new object[] { totalBytesReceived.ToArray() });
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
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        Task<HttpResponseMessage> response = httpClient.GetAsync("https://aditapi.azurewebsites.net/api/keys/" + jsonData["ID"]);
                        response.Wait();
                        var content = response.Result.Content.ReadAsStringAsync();
                        content.Wait();
                        if (string.IsNullOrWhiteSpace(content.Result))
                        {
                            throw new Exception("Response from API was empty.");
                        }
                        Encryption = new Encryption();
                        Encryption.Key = Convert.FromBase64String(content.Result);
                    }
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
