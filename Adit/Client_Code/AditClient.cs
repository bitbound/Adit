using Adit.Models;
using Adit.Pages;
using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Client_Code
{
    public class AditClient
    {
        public static TcpClient TcpClient { get; set; }

        public static ClientSocketMessages SocketMessageHandler { get; set; }

        private static int bufferSize = 9999999;
        public static int PartnersConnected { get; set; } = 0;

        public static string SessionID { get; set; }

        public static async Task Connect(ConnectionTypes connectionType)
        {
            if (TcpClient?.Client?.Connected == true)
            {
                throw new Exception("Client is already connected.");
            }
            TcpClient = new TcpClient();
            try
            {
                await TcpClient.ConnectAsync(Config.Current.ClientHost, Config.Current.ClientPort);
                TcpClient.Client.ReceiveBufferSize = bufferSize;
                TcpClient.Client.SendBufferSize = bufferSize;
            }
            catch
            {
                MessageBox.Show("Unable to connect.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SocketMessageHandler = new ClientSocketMessages(TcpClient.Client);
            WaitForServerMessage();
            SocketMessageHandler.SendConnectionType(connectionType);
        }

        private static void WaitForServerMessage()
        {
            var receiveBuffer = new byte[bufferSize];
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            socketArgs.Completed += ReceiveFromServerCompleted;
            TcpClient.Client.ReceiveAsync(socketArgs);
            ClientMain.Current.RefreshUICall();
        }


        private static void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                Utilities.WriteToLog($"Socket error in AditClient: {e.SocketError.ToString()}");
                SessionID = String.Empty;
                ClientMain.Current.RefreshUICall();
                return;
            }
            var result = SocketMessageHandler.ProcessSocketMessage(e.Buffer);
            if (!result)
            {
                TcpClient?.Client?.Close();
                return;
            }
            WaitForServerMessage();
        }
    }
}
