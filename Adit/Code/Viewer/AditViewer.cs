using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Viewer
{
    public static class AditViewer
    {
        private static SocketAsyncEventArgs socketArgs;
        private static byte[] receiveBuffer;
        public static TcpClient TcpClient { get; set; }

        public static string SessionID { get; set; }

        public static ViewerSocketMessages SocketMessageHandler { get; set; }

        private static int bufferSize = 9999999;

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

        public static async Task Connect(string sessionID)
        {
            if (IsConnected)
            {
                MessageBox.Show("The client is already connected.", "Already Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            TcpClient = new TcpClient();
            try
            {
                SessionID = sessionID;
                await TcpClient.ConnectAsync(Config.Current.ViewerHost, Config.Current.ViewerPort);
                TcpClient.Client.ReceiveBufferSize = bufferSize;
                TcpClient.Client.SendBufferSize = bufferSize;
                if (receiveBuffer == null)
                {
                    receiveBuffer = new byte[bufferSize];
                }
                socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                socketArgs.Completed += ReceiveFromServerCompleted;
            }
            catch
            {
                MessageBox.Show("Unable to connect.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Pages.Viewer.Current.RefreshUICall();
                return;
            }
            SocketMessageHandler = new ViewerSocketMessages(TcpClient.Client);
            WaitForServerMessage();
            SocketMessageHandler.SendConnectionType(ConnectionTypes.Viewer);
        }

        private static void WaitForServerMessage()
        {
            if (IsConnected)
            {
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
                Utilities.WriteToLog($"Socket error in AditClient: {e.SocketError.ToString()}");
                MainWindow.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow.Current.WindowState = WindowState.Normal;
                });
                Pages.Viewer.Current.RefreshUICall();
                return;
            }
            var result = SocketMessageHandler.ProcessSocketMessage(e);
            if (!result)
            {
                TcpClient?.Dispose();
                return;
            }
            WaitForServerMessage();
        }
    }
}
