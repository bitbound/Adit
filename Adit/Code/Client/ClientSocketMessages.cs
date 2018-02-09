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
        private void ReceivePartnerList(dynamic jsonData)
        {
            AditClient.PartnerList = ((object[])jsonData["PartnerList"]).Select(x=>x.ToString()).ToList();
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
                else
                {
                    // TODO: Resend after delay.
                }
            }

        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {

        }
    }
}
