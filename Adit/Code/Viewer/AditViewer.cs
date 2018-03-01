using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Viewer
{
    public static class AditViewer
    {
        public static TcpClient TcpClient { get; set; }

        public static string SessionID { get; set; }

        public static ViewerSocketMessages SocketMessageHandler { get; set; }


        public static int PartnersConnected { get; set; } = 0;
        public static bool RequestFullscreen { get; set; }
        public static List<string> ParticipantList { get; set; } = new List<string>();
        public static bool IsConnected
        {
            get
            {
                return TcpClient?.Client?.Connected == true;
            }
        }

        public static System.Drawing.Point NextDrawPoint { get; internal set; }

        public static async Task Connect(string sessionID)
        {
            if (IsConnected)
            {
                MessageBox.Show("The viewer is already connected.", "Already Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            TcpClient = new TcpClient();
            TcpClient.ReceiveBufferSize = Config.Current.BufferSize;
            TcpClient.SendBufferSize = Config.Current.BufferSize;
            try
            {
                SessionID = sessionID;
                await TcpClient.ConnectAsync(Config.Current.ViewerHost, Config.Current.ViewerPort);
                SocketMessageHandler = new ViewerSocketMessages(TcpClient.Client);
                WaitForServerMessage();
                if (Config.Current.IsClipboardShared)
                {
                    ClipboardManager.Current.BeginWatching(SocketMessageHandler);
                }
            }
            catch
            {
                MessageBox.Show("Unable to connect.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Pages.Viewer.Current.RefreshUICall();
                return;
            }
        }
        private static void WaitForServerMessage()
        {
            if (IsConnected)
            {
                var socketArgs = SocketArgsPool.GetReceiveArg();
                socketArgs.Completed += ReceiveFromServerCompleted;
                var willFireCallback = TcpClient.Client.ReceiveAsync(socketArgs);
                if (!willFireCallback)
                {
                    ReceiveFromServerCompleted(TcpClient.Client, socketArgs);
                }
                Pages.Viewer.Current.RefreshUICall();
            }
        }

        public static void Disconnect()
        {
            TcpClient.Close();
            Pages.Viewer.Current.RefreshUICall();
        }

        private static void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                e.Completed -= ReceiveFromServerCompleted;
                (e as SocketArgs).IsInUse = false;
                Utilities.WriteToLog($"Socket closed in AditViewer: {e.SocketError.ToString()}");
                MainWindow.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow.Current.WindowState = WindowState.Normal;
                });
                Pages.Viewer.Current.RefreshUICall();
                return;
            }
            SocketMessageHandler.ProcessSocketArgs(e, ReceiveFromServerCompleted, TcpClient.Client);
        }
    }
}
