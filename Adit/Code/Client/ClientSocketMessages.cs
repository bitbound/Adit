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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Win32_Classes;

namespace Adit.Code.Client
{
    public class ClientSocketMessages : SocketMessageHandler
    {
        Socket socketOut;
        Capturer capturer;
        public ClientSocketMessages(Socket socketOut)
            : base(socketOut)
        {
            this.socketOut = socketOut;
            this.capturer = new Capturer();
        }

        private void ReceiveSessionID(dynamic jsonData)
        {
            AditClient.SessionID = jsonData["SessionID"];
            Pages.Client.Current.RefreshUICall();
        }
        private void ReceiveParticipantList(dynamic jsonData)
        {
            var participantList = ((object[])jsonData["ParticipantList"]).Select(x => x.ToString()).ToList();
            if (participantList.Count > AditClient.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has connected.");
            }
            else if (participantList.Count < AditClient.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has disconnected.");
            }
            AditClient.ParticipantList = participantList;
            Pages.Client.Current.RefreshUICall();
        }

        private void ReceiveImageRequest(dynamic jsonData)
        {
            var requesterBytes = Encoding.UTF8.GetBytes(jsonData["RequesterID"]);
            capturer.CaptureScreen();
            if (jsonData["Fullscreen"])
            {
                using (var ms = capturer.GetFullscreenStream(requesterBytes))
                {
                    SendBytes(ms.ToArray());
                }
            }
            else
            {
                if (capturer.IsNewFrameDifferent())
                {
                    using (var ms = capturer.GetDiffStream(requesterBytes))
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
        private void ReceiveMouseMove(dynamic jsonData)
        {
            User32.SetCursorPos((int)Math.Round((double)jsonData["X"] * capturer.TotalWidth) + capturer.OffsetX, 
                                (int)Math.Round((double)jsonData["Y"] * capturer.TotalHeight) + capturer.OffsetY);
        }
        private void ReceiveClearAllKeys(dynamic jsonData)
        {
            MainWindow.Current.Dispatcher.Invoke(() =>
            {
                foreach (var key in Enum.GetNames(typeof(Key)))
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
        private void ReceiveMouseWheel(dynamic jsonData)
        {
            User32.SendMouseWheel(jsonData["Delta"]);
        }
        private void ReceiveMouseLeftDown(dynamic jsonData)
        {
            User32.SendLeftMouseDown(
                    (int)Math.Round((double)jsonData["X"] * capturer.TotalWidth) + capturer.OffsetX,
                    (int)Math.Round((double)jsonData["Y"] * capturer.TotalHeight) + capturer.OffsetY
                );
        }
        private void ReceiveMouseLeftUp(dynamic jsonData)
        {
            User32.SendLeftMouseUp(
                   (int)Math.Round((double)jsonData["X"] * capturer.TotalWidth) + capturer.OffsetX,
                   (int)Math.Round((double)jsonData["Y"] * capturer.TotalHeight) + capturer.OffsetY
               );
        }
        private void ReceiveMouseRightDown(dynamic jsonData)
        {
            User32.SendRightMouseDown(
                  (int)Math.Round((double)jsonData["X"] * capturer.TotalWidth) + capturer.OffsetX,
                  (int)Math.Round((double)jsonData["Y"] * capturer.TotalHeight) + capturer.OffsetY
              );
        }
        private void ReceiveMouseRightUp(dynamic jsonData)
        {
            User32.SendRightMouseUp(
                 (int)Math.Round((double)jsonData["X"] * capturer.TotalWidth) + capturer.OffsetX,
                 (int)Math.Round((double)jsonData["Y"] * capturer.TotalHeight) + capturer.OffsetY
             );
        }
    }
}
