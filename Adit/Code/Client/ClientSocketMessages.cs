using Adit.Code.Shared;
using Adit.Models;
using Adit.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Adit.Code.Client
{
    public class ClientSocketMessages : SocketMessageHandler
    {
        Socket socketOut;
        public ClientSocketMessages(Socket socketOut)
            : base(socketOut)
        {
            this.socketOut = socketOut;
        }

        private void ReceiveSessionID(dynamic jsonData)
        {
            AditClient.SessionID = jsonData["SessionID"];
            Pages.Client.Current.RefreshUICall();
        }
        private void ReceivePartnerCount(dynamic jsonData)
        {
            AditClient.PartnersConnected = jsonData["PartnerCount"];
            Pages.Client.Current.RefreshUICall();
        }

        private void ReceiveImageRequest(dynamic jsonData)
        {
            var requesterBytes = Encoding.UTF8.GetBytes(jsonData["RequesterID"]);
            Capturer.Current.CaptureScreen();
            if (jsonData["Fullscreen"])
            {
                using (var ms = Capturer.Current.GetFullscreenStream(requesterBytes))
                {
                    SendBytes(ms.ToArray());
                }
            }
            else
            {
                if (Capturer.Current.IsNewFrameDifferent())
                {
                    using (var ms = Capturer.Current.GetDiffStream(requesterBytes))
                    {
                        SendBytes(ms.ToArray());
                    }
                }
            }
        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {

        }
    }
}
