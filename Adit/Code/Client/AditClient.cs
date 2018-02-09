using Adit.Models;
using Adit.Pages;
using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Client
{
    public class AditClient
    {
        private static SocketAsyncEventArgs socketArgs;
        private static byte[] receiveBuffer;
        public static TcpClient TcpClient { get; set; }

        public static ClientSocketMessages SocketMessageHandler { get; set; }

        private static int bufferSize = 9999999;
        public static List<string> PartnerList { get; set; } = new List<string>();

        public static string SessionID { get; set; }

        public static async Task Connect()
        {
            if (TcpClient?.Client?.Connected == true)
            {
                MessageBox.Show("The client is already connected.", "Already Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            TcpClient = new TcpClient();
            try
            {
                await TcpClient.ConnectAsync(Config.Current.ClientHost, Config.Current.ClientPort);
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
                Pages.Client.Current.RefreshUICall();
                return;
            }
            SocketMessageHandler = new ClientSocketMessages(TcpClient.Client);
            WaitForServerMessage();
            SocketMessageHandler.SendConnectionType(ConnectionTypes.Client);
        }

        private static void WaitForServerMessage()
        {
            var willFireCallback = TcpClient.Client.ReceiveAsync(socketArgs);
            if (!willFireCallback)
            {
                ReceiveFromServerCompleted(TcpClient.Client, socketArgs);
            }
            Pages.Client.Current.RefreshUICall();
        }


        private static void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Utilities.WriteToLog($"Socket error in AditClient: {e.SocketError.ToString()}");
                SessionID = String.Empty;
                Pages.Client.Current.RefreshUICall();
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
