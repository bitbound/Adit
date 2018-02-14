using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Adit_Service
{
    class ServiceSocketMessages
    {
        Socket socketOut;
        int totalHeight = SystemInformation.VirtualScreen.Height;
        int totalWidth = SystemInformation.VirtualScreen.Width;
        int offsetX = SystemInformation.VirtualScreen.Left;
        int offsetY = SystemInformation.VirtualScreen.Top;
        public ServiceSocketMessages(Socket socketOut)
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
                try
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
                    }
                }
                catch (Exception ex)
                {
                    Utilities.WriteToLog(ex);
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
        public void SendHeartbeat()
        {
            try
            {
                if (!AditService.IsConnected)
                {
                    AditService.Connect();
                }
                var uptime = new PerformanceCounter("System", "System Up Time", true);
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

                // Send notification to server that this connection is for a client service.
                var request = new
                {
                    Type = "Heartbeat",
                    ComputerName = Environment.MachineName,
                    CurrentUser = currentUser,
                    LastReboot = (DateTime.Now - TimeSpan.FromSeconds(uptime.NextValue()))
                };
                SendJSON(request);
                var di = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + @"\InstaTech\");
                foreach (var file in di.GetFiles())
                {
                    if (DateTime.Now - file.LastWriteTime > TimeSpan.FromDays(1))
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            Utilities.WriteToLog(ex);
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
