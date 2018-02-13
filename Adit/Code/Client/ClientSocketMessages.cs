﻿using Adit.Code.Shared;
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
        private void ReceiveMouseMove(dynamic jsonData)
        {
            User32.SetCursorPos((int)Math.Round((double)jsonData["X"] * Capturer.Current.TotalWidth) + Capturer.Current.OffsetX, 
                                (int)Math.Round((double)jsonData["Y"] * Capturer.Current.TotalHeight) + Capturer.Current.OffsetY);
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
                    (int)Math.Round((double)jsonData["X"] * Capturer.Current.TotalWidth) + Capturer.Current.OffsetX,
                    (int)Math.Round((double)jsonData["Y"] * Capturer.Current.TotalHeight) + Capturer.Current.OffsetY
                );
        }
        private void ReceiveMouseLeftUp(dynamic jsonData)
        {
            User32.SendLeftMouseUp(
                   (int)Math.Round((double)jsonData["X"] * Capturer.Current.TotalWidth) + Capturer.Current.OffsetX,
                   (int)Math.Round((double)jsonData["Y"] * Capturer.Current.TotalHeight) + Capturer.Current.OffsetY
               );
        }
        private void ReceiveMouseRightDown(dynamic jsonData)
        {
            User32.SendRightMouseDown(
                  (int)Math.Round((double)jsonData["X"] * Capturer.Current.TotalWidth) + Capturer.Current.OffsetX,
                  (int)Math.Round((double)jsonData["Y"] * Capturer.Current.TotalHeight) + Capturer.Current.OffsetY
              );
        }
        private void ReceiveMouseRightUp(dynamic jsonData)
        {
            User32.SendRightMouseUp(
                 (int)Math.Round((double)jsonData["X"] * Capturer.Current.TotalWidth) + Capturer.Current.OffsetX,
                 (int)Math.Round((double)jsonData["Y"] * Capturer.Current.TotalHeight) + Capturer.Current.OffsetY
             );
        }
    }
}