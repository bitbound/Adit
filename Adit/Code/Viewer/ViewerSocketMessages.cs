using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                SendImageRequest(true);
                Pages.Viewer.Current.RefreshUICall();
            }
        }
        private void ReceiveReadyForViewer(dynamic jsonData)
        {
            SendViewerConnectRequest();
        }
        public void SendImageRequest(bool fullscreen)
        {
            SendJSON(new
            {
                Type = "ImageRequest",
                Fullscreen = fullscreen
            });
        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {
            var metadata = bytesReceived.Take(6).ToArray();
            var xPosition = metadata[0] * 10000 + metadata[1] * 100 + metadata[2];
            var yPosition = metadata[3] * 10000 + metadata[4] * 100 + metadata[5];
            var startDrawingPoint = new Point(xPosition, yPosition);
            var imageData = bytesReceived.Skip(6).ToArray();
            Pages.Viewer.Current.DrawImageCall(startDrawingPoint, imageData);
        }
    }
}
