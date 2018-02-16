using Adit.Code.Shared;
using Adit.Controls;
using Adit.Models;
using System;
using System.Collections.Generic;
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
            if (Config.Current.MaximizeViewerOnConnect)
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
            throw new NotImplementedException();
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

        internal void SendFileTransfer(string fileName)
        {
            throw new NotImplementedException();
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
            var metadata = bytesReceived.Take(6).ToArray();
            var xPosition = metadata[0] * 10000 + metadata[1] * 100 + metadata[2];
            var yPosition = metadata[3] * 10000 + metadata[4] * 100 + metadata[5];
            var startDrawingPoint = new Point(xPosition, yPosition);
            Pages.Viewer.Current.DrawImageCall(startDrawingPoint, bytesReceived.Skip(6));
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

    }
}
