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
                SendBytes(bytes);
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
        public async Task ProcessSocketMessage(SocketAsyncEventArgs socketArgs)
        {
            if (socketArgs.BytesTransferred == 0)
            {
                return;
            }
            try
            {
                var receivedBytes = socketArgs.Buffer.Take(socketArgs.BytesTransferred).ToArray();
                if (Encryption != null)
                {
                    receivedBytes = await Encryption.DecryptBytes(receivedBytes);
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
