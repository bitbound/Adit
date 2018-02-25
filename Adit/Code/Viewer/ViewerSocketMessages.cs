using Adit.Code.Shared;
using Adit.Controls;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Adit.Code.Viewer
{
    public class ViewerSocketMessages : SocketMessageHandler
    {
        Socket socketOut;
        List<byte> BinaryTransferBuffer { get; set; } = new List<byte>();
        public ViewerSocketMessages(Socket socketOut)
            : base(socketOut)
        {
            this.socketOut = socketOut;
        }

        public async Task SendViewerConnectRequest()
        {
            await SendJSON(new
            {
                Type = "ViewerConnectRequest",
                SessionID = AditViewer.SessionID
            });
        }

        public async Task SendMouseMove(double x, double y)
        {
            await SendJSON(new
            {
                Type = "MouseMove",
                X = x,
                Y = y
            });
        }
        private void ReceiveBinaryTransferStarting(dynamic jsonData)
        {
            ExpectedBinarySize = jsonData["Size"];
            LastRequesterID = jsonData["Sender"];
            ReceiveTransferType = Enum.Parse(typeof(BinaryTransferType), jsonData["TransferType"]);
            AditViewer.NextDrawPoint = new System.Drawing.Point(jsonData["ExtraData"]["X"], jsonData["ExtraData"]["Y"]);
        }
        private async void ReceiveViewerConnectRequest(dynamic jsonData)
        {
            if (jsonData["Status"] == "notfound")
            {
                MessageBox.Show("The session ID wasn't found.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                AditViewer.TcpClient.Close();
                Pages.Viewer.Current.RefreshUICall();
                return;
            }
            else if (jsonData["Status"] == "ok")
            {
                AditViewer.RequestFullscreen = true;
                Pages.Viewer.Current.RefreshUICall();
                await SendImageRequest();
            }
        }
        private async void ReceiveEncryptionStatus(dynamic jsonData)
        {
            try
            {
                if (jsonData["Status"] == "On")
                {
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var response = await httpClient.GetAsync("https://aditapi.azurewebsites.net/api/keys/" + jsonData["ID"]);
                        var content = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            throw new Exception("Response from API was empty.");
                        }
                        Encryption = new Encryption();
                        Encryption.Key = Convert.FromBase64String(content);
                    }
                }
                else if (jsonData["Status"] == "Failed")
                {
                    throw new Exception("Server failed to start encrypted connection.");
                }
                await SendConnectionType(ConnectionTypes.Viewer);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                System.Windows.MessageBox.Show("There was a problem starting an encrypted connection.  If the issue persists, please contact support.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                MainWindow.Current.Dispatcher.Invoke(() => {
                    App.Current.Shutdown();
                });
            }
        }

        public async Task SendKeyDown(Key key)
        {
            await SendJSON(new
            {
                Type = "KeyDown",
                Key = key.ToString()
            });
        }

        public async Task SendKeyUp(Key key)
        {
            await SendJSON(new
            {
                Type = "KeyUp",
                Key = key.ToString()
            });
        }

        public async Task SendClearAllKeys()
        {
            await SendJSON(new
            {
                Type = "ClearAllKeys"
            });
        }

        public async Task SendMouseWheel(int delta)
        {
            await SendJSON(new
            {
                Type = "MouseWheel",
                Delta = delta
            });
        }

        private async void ReceiveReadyForViewer(dynamic jsonData)
        {
            if (Config.Current.IsViewerMaximizedOnConnect)
            {
                MainWindow.Current.Dispatcher.Invoke(() => 
                {
                    MainWindow.Current.WindowState = WindowState.Maximized;
                });
            }
            await SendViewerConnectRequest();
        }

        public async Task SendMouseLeftDown(double x, double y)
        {
            await SendJSON(new
            {
                Type = "MouseLeftDown",
                X = x,
                Y = y
            });
        }

        public async Task SendMouseLeftUp(double x, double y)
        {
            await SendJSON(new
            {
                Type = "MouseLeftUp",
                X = x,
                Y = y
            });
        }

        public async Task SendCtrlAltDel()
        {
            await SendJSON(new
            {
                Type = "CtrlAltDel"
            });
        }

        private async void ReceiveNoScreenActivity(dynamic jsonData)
        {
            await SendImageRequest();
        }
        public async Task SendImageRequest()
        {
            await SendJSON(new
            {
                Type = "ImageRequest",
                Fullscreen = AditViewer.RequestFullscreen
            });
            AditViewer.RequestFullscreen = false;
        }

        public async Task SendFileTransfer(string fileName)
        {
            var fileSize = new FileInfo(fileName).Length;
            await SendJSON(new
            {
                Type = "FileTransfer",
                FileSize = fileSize,
                FileName = Path.GetFileName(fileName),
                FullPath = fileName
            });
        }

        private async void ReceiveFileTransfer(dynamic jsonData)
        {
            SendBytes(File.ReadAllBytes(jsonData["FullPath"]));
            await SendImageRequest();
        }

        public async Task SendMouseRightDown(double x, double y)
        {
            await SendJSON(new
            {
                Type = "MouseRightDown",
                X = x,
                Y = y
            });
        }

        public async Task SendMouseRightUp(double x, double y)
        {
            await SendJSON(new
            {
                Type = "MouseRightUp",
                X = x,
                Y = y
            });
        }

        private async void ReceiveByteArray(byte[] bytesReceived)
        {
            if (bytesReceived.Length == ExpectedBinarySize)
            {
                await Pages.Viewer.Current.DrawImageCall(bytesReceived);
                BinaryTransferBuffer.Clear();
                BinaryTransferBuffer.TrimExcess();
            }
            else if (bytesReceived.Length > ExpectedBinarySize)
            {
                Utilities.WriteToLog("Received bytes exceeded expected size.");
                BinaryTransferBuffer.Clear();
                BinaryTransferBuffer.TrimExcess();
            }
            else
            {
                BinaryTransferBuffer.AddRange(bytesReceived);
                if (BinaryTransferBuffer.Count == ExpectedBinarySize)
                {
                    await Pages.Viewer.Current.DrawImageCall(BinaryTransferBuffer.ToArray());
                    BinaryTransferBuffer.Clear();
                    BinaryTransferBuffer.TrimExcess();
                }
                else if (BinaryTransferBuffer.Count > ExpectedBinarySize)
                {
                    Utilities.WriteToLog("Received bytes exceeded expected size.");
                    BinaryTransferBuffer.Clear();
                    BinaryTransferBuffer.TrimExcess();
                }
            }
        }

        private void ReceiveParticipantList(dynamic jsonData)
        {
            var participantList = ((object[])jsonData["ParticipantList"]).Select(x => x.ToString()).ToList();
            if (participantList.Count == 1)
            {
                FlyoutNotification.Show("Connection to partner has been closed.");
                AditViewer.Disconnect();
            }
            else if (participantList.Count > AditViewer.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has connected.");
            }
            else if (participantList.Count < AditViewer.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has disconnected.");
            }
            AditViewer.ParticipantList = participantList;
        }
        private void ReceiveRequestForElevatedClient(dynamic jsonData)
        {
            if (jsonData["Status"] == "failed")
            {
                MessageBox.Show("Failed to connect to client.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (jsonData["Status"] == "ok")
            {
                AditViewer.Disconnect();
                AditViewer.Connect(jsonData["ClientSessionID"]);
            }
        }

        private async void ReceiveDesktopSwitch(dynamic jsonData)
        {
            if (jsonData["Status"] == "ok")
            {
                AditViewer.RequestFullscreen = true;
                await SendImageRequest();
            }
            else if (jsonData["Status"] == "failed")
            {
                MessageBox.Show("The remote screen capture failed due to a desktop switch (i.e. switched to lock screen, UAC screen, etc.).  You may need to disconnect and reconnect.", "Remote Capture Stopped", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
