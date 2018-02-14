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
        int totalHeight = SystemInformation.VirtualScreen.Height;
        int totalWidth = SystemInformation.VirtualScreen.Width;
        int offsetX = SystemInformation.VirtualScreen.Left;
        int offsetY = SystemInformation.VirtualScreen.Top;
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
            var participantList = ((object[])jsonData["ParticipantList"]).Select(x => x.ToString()).ToList();
            if (participantList.Count > AditClient.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has connected.");
            }
            else if (participantList.Count < AditClient.ParticipantList.Count)
            {
                FlyoutNotification.Show("A partner has disconnected.");
            }
            AditClient.ParticipantList.RemoveAll(x => !participantList.Contains(x.ID));
            foreach (var partner in participantList)
            {
                if (!AditClient.ParticipantList.Exists(x=>x.ID == partner))
                {
                    AditClient.ParticipantList.Add(new Participant() { ID = partner });
                }
            }
            Pages.Client.Current.RefreshUICall();
        }

        private void ReceiveImageRequest(dynamic jsonData)
        {
            var requester = AditClient.ParticipantList.Find(x => x.ID == jsonData["RequesterID"]);
            if (requester.CaptureInstance == null)
            {
                requester.CaptureInstance = new Capturer();
            }
            var requesterBytes = Encoding.UTF8.GetBytes(jsonData["RequesterID"]);
            try
            {
                lock (requester.CaptureInstance)
                {
                    requester.CaptureInstance.CaptureScreen();
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(100);
                    ReceiveImageRequest(jsonData);
                });
            }
            if (jsonData["Fullscreen"])
            {
                using (var ms = requester.CaptureInstance.GetFullscreenStream(requesterBytes))
                {
                    SendBytes(ms.ToArray());
                }
            }
            else
            {
                if (requester.CaptureInstance.IsNewFrameDifferent())
                {
                    using (var ms = requester.CaptureInstance.GetDiffStream(requesterBytes))
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
            User32.SetCursorPos((int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX, 
                                (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY);
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
                    (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                    (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
                );
        }
        private void ReceiveMouseLeftUp(dynamic jsonData)
        {
            User32.SendLeftMouseUp(
                   (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                   (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
               );
        }
        private void ReceiveMouseRightDown(dynamic jsonData)
        {
            User32.SendRightMouseDown(
                  (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                  (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
              );
        }
        private void ReceiveMouseRightUp(dynamic jsonData)
        {
            User32.SendRightMouseUp(
                 (int)Math.Round((double)jsonData["X"] * totalWidth) + offsetX,
                 (int)Math.Round((double)jsonData["Y"] * totalHeight) + offsetY
             );
        }
    }
}
