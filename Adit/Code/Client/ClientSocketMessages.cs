using Adit.Code.Shared;
using Adit.Controls;
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
using System.Windows.Controls.Primitives;
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
        private void ReceiveParticipantList(dynamic jsonData)
        {
            var ParticipantList = ((object[])jsonData["ParticipantList"]).Select(x => x.ToString()).ToList();
            if (ParticipantList.Count > AditClient.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has connected.");
            }
            else if (ParticipantList.Count < AditClient.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has disconnected.");
            }
            AditClient.ParticipantList = ((object[])jsonData["ParticipantList"]).Select(x=>x.ToString()).ToList();
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
                    Task.Run(() =>
                    {
                        System.Threading.Thread.Sleep(100);
                        ReceiveImageRequest(jsonData);
                    });
                }
            }

        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {

        }
    }
}
