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

        public void SendViewerConnectRequest()
        {
            SendJSON(new
            {
                Type = "ViewerConnectRequest",
                SessionID = AditViewer.SessionID
            });
        }

        public void SendMouseMove(double x, double y)
        {
            SendJSON(new
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
        private void ReceiveViewerConnectRequest(dynamic jsonData)
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
                SendImageRequest();
            }
        }

        public void SendKeyDown(Key key)
        {
            SendJSON(new
            {
                Type = "KeyDown",
                Key = key.ToString()
            });
        }

        public void SendKeyUp(Key key)
        {
            SendJSON(new
            {
                Type = "KeyUp",
                Key = key.ToString()
            });
        }

        public void SendClearAllKeys()
        {
            SendJSON(new
            {
                Type = "ClearAllKeys"
            });
        }

        public void SendMouseWheel(int delta)
        {
            SendJSON(new
            {
                Type = "MouseWheel",
                Delta = delta
            });
        }

        private void ReceiveReadyForViewer(dynamic jsonData)
        {
            if (Config.Current.IsViewerMaximizedOnConnect)
            {
                MainWindow.Current.Dispatcher.Invoke(() => 
                {
                    MainWindow.Current.WindowState = WindowState.Maximized;
                });
            }
            SendViewerConnectRequest();
        }

        public void SendMouseLeftDown(double x, double y)
        {
            SendJSON(new
            {
                Type = "MouseLeftDown",
                X = x,
                Y = y
            });
        }

        public void SendMouseLeftUp(double x, double y)
        {
            SendJSON(new
            {
                Type = "MouseLeftUp",
                X = x,
                Y = y
            });
        }

        internal void SendCtrlAltDel()
        {
            SendJSON(new
            {
                Type = "CtrlAltDel"
            });
        }

        private void ReceiveNoScreenActivity(dynamic jsonData)
        {
            SendImageRequest();
        }
        public void SendImageRequest()
        {
            SendJSON(new
            {
                Type = "ImageRequest",
                Fullscreen = AditViewer.RequestFullscreen
            });
            AditViewer.RequestFullscreen = false;
        }

        public void SendFileTransfer(string fileName)
        {
            var fileSize = new FileInfo(fileName).Length;
            SendJSON(new
            {
                Type = "FileTransfer",
                FileSize = fileSize,
                FileName = Path.GetFileName(fileName),
                FullPath = fileName
            });
        }

        private void ReceiveFileTransfer(dynamic jsonData)
        {
            SendBytes(File.ReadAllBytes(jsonData["FullPath"]));
            SendImageRequest();
        }

        internal void SendMouseRightDown(double x, double y)
        {
            SendJSON(new
            {
                Type = "MouseRightDown",
                X = x,
                Y = y
            });
        }

        internal void SendMouseRightUp(double x, double y)
        {
            SendJSON(new
            {
                Type = "MouseRightUp",
                X = x,
                Y = y
            });
        }

        private void ReceiveByteArray(byte[] bytesReceived)
        {
            if (bytesReceived.Length == ExpectedBinarySize)
            {
                Pages.Viewer.Current.DrawImageCall(bytesReceived);
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
                    Pages.Viewer.Current.DrawImageCall(BinaryTransferBuffer.ToArray());
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

        private void ReceiveDesktopSwitch(dynamic jsonData)
        {
            if (jsonData["Status"] == "ok")
            {
                AditViewer.RequestFullscreen = true;
                SendImageRequest();
            }
            else if (jsonData["Status"] == "failed")
            {
                MessageBox.Show("The remote screen capture failed due to a desktop switch (i.e. switched to lock screen, UAC screen, etc.).  You may need to disconnect and reconnect.", "Remote Capture Stopped", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
