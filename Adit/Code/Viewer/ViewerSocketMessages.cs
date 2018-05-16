using Adit.Code.Shared;
using Adit.Controls;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Win32_Classes;

namespace Adit.Code.Viewer
{
    public class ViewerSocketMessages : SocketMessageHandler
    {
        Socket socketOut;
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
                Pages.Viewer.Current.RefreshUICall();
                SendCaptureRequest();
            }
        }

        private void ReceiveEncryptionStatus(dynamic jsonData)
        {
            try
            {
                if (jsonData["Status"] == "On")
                {
                    Encryptor = new Encryption();
                    Encryptor.Key = Encryption.GetClientKey();
                    if (Encryptor.Key == null)
                    {
                        AditViewer.Disconnect();
                        Pages.Viewer.Current.RefreshUICall();
                        return;
                    }
                }
                else if (jsonData["Status"] == "Failed")
                {
                    throw new Exception("Server failed to start encrypted connection.");
                }
                SendConnectionType(ConnectionTypes.Viewer);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                System.Windows.MessageBox.Show("There was a problem starting an encrypted connection.  If the issue persists, please contact support.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        internal void SendStopCapture()
        {
            SendJSON(new
            {
                Type = "StopCapture"
            });
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
                Delta = delta.ToString()
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

        public void SendCtrlAltDel()
        {
             SendJSON(new
            {
                Type = "CtrlAltDel"
            });
        }

        public void SendFullscreenRequest()
        {
            SendJSON(new
            {
                Type = "FullscreenRequest"
            });
        }

        public void SendCaptureRequest()
        {
            
            SendJSON(new
            {
                Type = "CaptureRequest"
            });
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
            byte[] fileBytes = File.ReadAllBytes(jsonData["FullPath"]);
            var messageHeader = new byte[1];
            messageHeader[0] = 2;
            SendBytes(messageHeader.Concat(fileBytes).ToArray(), string.Empty, string.Empty);
        }

        public void SendMouseRightDown(double x, double y)
        {
            SendJSON(new
            {
                Type = "MouseRightDown",
                X = x,
                Y = y
            });
        }

        public void SendMouseRightUp(double x, double y)
        {
            SendJSON(new
            {
                Type = "MouseRightUp",
                X = x,
                Y = y
            });
        }
        private void ReceiveIconUpdate(dynamic jsonData)
        {
            var iconHandle = jsonData["Icon"];
            var cursorName = CursorMap.AllCursors.Find(x => x.CursorHandle == iconHandle).WindowsCursorName;
            var cursor = (Cursor)typeof(Cursors).GetProperties().FirstOrDefault(x => x.Name == cursorName).GetValue(null);
            Pages.Viewer.Current.Dispatcher.Invoke(() =>
            {
                Pages.Viewer.Current.DrawingSurface.Cursor = cursor;
            });
        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {
            if (bytesReceived[0] == 1)
            {
                var xPosition = bytesReceived[1] * 10000 + bytesReceived[2] * 100 + bytesReceived[3];
                var yPosition = bytesReceived[4] * 10000 + bytesReceived[5] * 100 + bytesReceived[6];
                AditViewer.NextDrawPoint = new System.Drawing.Point(xPosition, yPosition);

                Pages.Viewer.Current.DrawImageCall(bytesReceived.Skip(7).ToArray());
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
                AditViewer.Disconnect();
            }
            else if (jsonData["Status"] == "ok")
            {
                AditViewer.Disconnect();
                AditViewer.Connect(jsonData["ClientSessionID"]);
            }
        }
        private void ReceiveClipboardTransfer(dynamic jsonData)
        {
            if (jsonData["SourceComputer"] == Environment.MachineName)
            {
                return;
            }
            if (jsonData["Format"] == "FileDrop")
            {
                ClipboardManager.Current.SetFiles(jsonData["FileNames"], jsonData["FileContents"]);
            }
            else
            {
                ClipboardManager.Current.SetData(jsonData["Format"], jsonData["Data"]);
            }
        }
        private void ReceiveDesktopSwitch(dynamic jsonData)
        {
            if (jsonData["Status"] == "ok")
            {
                AditViewer.SocketMessageHandler.SendCaptureRequest();
            }
            else if (jsonData["Status"] == "failed")
            {
                MessageBox.Show("The remote screen capture failed due to a desktop switch (i.e. switched to lock screen, UAC screen, etc.).  You may need to disconnect and reconnect.", "Remote Capture Stopped", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
