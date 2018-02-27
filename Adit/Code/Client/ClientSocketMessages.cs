using Adit.Code.Shared;
using Adit.Controls;
using Adit.Models;
using Adit.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Win32_Classes;

namespace Adit.Code.Client
{
    public class ClientSocketMessages : SocketMessageHandler
    {
        string fileTransferName;
        long fileTransferSize;
        int offsetX = SystemInformation.VirtualScreen.Left;
        int offsetY = SystemInformation.VirtualScreen.Top;
        Socket socketOut;
        int totalHeight = SystemInformation.VirtualScreen.Height;
        int totalWidth = SystemInformation.VirtualScreen.Width;
        public ClientSocketMessages(Socket socketOut)
            : base(socketOut)
        {
            this.socketOut = socketOut;
        }

        public void SendConnectionType(ConnectionTypes connectionType, string sessionIDToUse)
        {
            SendJSON(new
            {
                Type = "ConnectionType",
                ConnectionType = connectionType.ToString(),
                SessionID = sessionIDToUse
            });
        }

        public void SendDesktopSwitch()
        {
            var request = new
            {
                Type = "DesktopSwitch"
            };
            SendJSON(request);
        }

        private void ReceiveByteArray(byte[] bytesReceived)
        {
            try
            {
                if (bytesReceived[0] == 2)
                {
                    using (var fs = new FileStream(Path.Combine(Utilities.FileTransferFolder, fileTransferName), FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        fs.Position = fs.Length;
                        fs.Write(bytesReceived.Skip(1).ToArray(), 0, bytesReceived.Skip(1).Count());
                        if (fs.Length >= fileTransferSize)
                        {
                            Process.Start(Utilities.FileTransferFolder);
                        }
                    }
                }
            }
            finally
            {
                fileTransferName = null;
                fileTransferSize = 0;
                SendNoScreenActivity();
            }
        }

        private void ReceiveClearAllKeys(dynamic jsonData)
        {
            MainWindow.Current.Dispatcher.Invoke(() =>
            {
                foreach (var key in Enum.GetNames(typeof(Key)).Where(x => x != "None"))
                {
                    try
                    {
                        if (Keyboard.IsKeyDown((Key)Enum.Parse(typeof(Key), key)))
                        {
                            User32.SendKeyUp((User32.VirtualKeyShort)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key)));
                        }
                    }
                    catch { }

                }
            });

        }

        private void ReceiveClipboardTransfer(dynamic jsonData)
        {
            if (jsonData["Format"] == "FileDrop")
            {
                ClipboardManager.Current.SetFiles(jsonData["FileNames"], jsonData["FileContents"]);
            }
            else
            {
                ClipboardManager.Current.SetData(jsonData["Format"], jsonData["Data"]);
            }
        }

        private void ReceiveCtrlAltDel(dynamic jsonData)
        {
            if (Process.GetProcessesByName("Adit_Service").Count() > 0)
            {
                var request = new
                {
                    Type = "SAS",
                    MAC = Utilities.GetMACAddress()
                };
                SendJSON(request);
            }
            else
            {
                User32.SendSAS(true);
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
                if (AditClient.ConnectionType == ConnectionTypes.Client)
                {
                    SendConnectionType(ConnectionTypes.Client);
                }
                else if (AditClient.ConnectionType == ConnectionTypes.ElevatedClient)
                {
                    SendConnectionType(ConnectionTypes.ElevatedClient, AditClient.SessionID);
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                if (Config.Current.StartupMode != Config.StartupModes.Notifier)
                {
                    System.Windows.MessageBox.Show("There was a problem starting an encrypted connection.  If the issue persists, please contact support.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                MainWindow.Current.Dispatcher.Invoke(() => {
                    App.Current.Shutdown();
                });
            }
        }


        private void ReceiveFileTransfer(dynamic jsonData)
        {
            var targetFile = new FileInfo(Path.Combine(Utilities.FileTransferFolder, jsonData["FileName"]));
            if (targetFile.Exists)
            {
                targetFile.Delete();
            }
            fileTransferName = jsonData["FileName"];
            fileTransferSize = jsonData["FileSize"];
            SendJSON(jsonData);
        }

        private void ReceiveImageRequest(dynamic jsonData)
        {
            try
            {
                var requester = AditClient.ParticipantList.Find(x => x.ID == jsonData["RequesterID"]);
                if (requester == null)
                {
                    return;
                }
                if (requester.CaptureInstance == null)
                {
                    requester.CaptureInstance = new Capturer();
                }
                requester.CaptureInstance.CaptureScreen();
                if (jsonData["Fullscreen"])
                {
                    SendBytes(requester.CaptureInstance.GetCapture(true));
                }
                else
                {
                    if (requester.CaptureInstance.IsNewFrameDifferent())
                    {
                        SendBytes(requester.CaptureInstance.GetCapture(false));
                    }
                    else
                    {
                        SendNoScreenActivity();
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(100);
                    ReceiveImageRequest(jsonData);
                });
            }
        }

        private void ReceiveKeyDown(dynamic jsonData)
        {
            var key = jsonData["Key"];
            User32.SendKeyDown((User32.VirtualKeyShort)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key)));
        }

        private void ReceiveKeyUp(dynamic jsonData)
        {
            var key = jsonData["Key"];
            User32.SendKeyUp((User32.VirtualKeyShort)KeyInterop.VirtualKeyFromKey((Key)Enum.Parse(typeof(Key), key)));
        }

        private void ReceiveMouseLeftDown(dynamic jsonData)
        {
            User32.SendLeftMouseDown(
                    (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                    (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
                );
        }

        private void ReceiveMouseLeftUp(dynamic jsonData)
        {
            User32.SendLeftMouseUp(
                   (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                   (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
               );
        }

        private void ReceiveMouseMove(dynamic jsonData)
        {
            User32.SetCursorPos((int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                                (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY);
        }

        private void ReceiveMouseRightDown(dynamic jsonData)
        {
            User32.SendRightMouseDown(
                  (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                  (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
              );
        }

        private void ReceiveMouseRightUp(dynamic jsonData)
        {
            User32.SendRightMouseUp(
                 (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                 (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
             );
        }

        private void ReceiveMouseWheel(dynamic jsonData)
        {
            User32.SendMouseWheel(jsonData["Delta"]);
        }

        private void ReceiveParticipantList(dynamic jsonData)
        {
            var participantList = ((object[])jsonData["ParticipantList"]).Select(x => x.ToString()).ToList();
            if (Config.Current.StartupMode != Config.StartupModes.Notifier)
            {
                if (participantList.Count > AditClient.ParticipantList.Count)
                {
                    FlyoutNotification.Show("A partner has connected.");
                }
                else if (participantList.Count < AditClient.ParticipantList.Count)
                {
                    FlyoutNotification.Show("A partner has disconnected.");
                }
            }
            AditClient.ParticipantList.RemoveAll(x => !participantList.Contains(x.ID));
            foreach (var partner in participantList)
            {
                if (!AditClient.ParticipantList.Exists(x => x.ID == partner))
                {
                    AditClient.ParticipantList.Add(new Participant() { ID = partner });
                }
            }
            Pages.Client.Current.RefreshUICall();
        }

        private void ReceiveSessionID(dynamic jsonData)
        {
            AditClient.SessionID = jsonData["SessionID"];
            Pages.Client.Current.RefreshUICall();
        }
        private void SendNoScreenActivity()
        {
            SendJSON(new
            {
                Type = "NoScreenActivity"
            });
        }
    }
}
