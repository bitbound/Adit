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
            Send(new
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
        private void SendImageRequest(bool fullscreen)
        {
            Send(new
            {
                Type = "ImageRequest",
                Fullscreen = fullscreen
            });
        }

    }
}
