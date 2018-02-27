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
using System.Diagnostics;

namespace Adit.Code.Client
{
    public class AditClient
    {
        public static ConnectionTypes ConnectionType { get; set; }
        public static bool DesktopSwitchPending { get; set; }
        public static bool IsConnected
        {
            get
            {
                return TcpClient?.Client?.Connected == true;
            }
        }

        public static List<Participant> ParticipantList { get; set; } = new List<Participant>();
        public static string SessionID { get; set; }
        public static ClientSocketMessages SocketMessageHandler { get; set; }
        public static TcpClient TcpClient { get; set; }
        public static void Connect()
        {
            ConnectionType = ConnectionTypes.Client;
            if (InitConnection())
            {
                //SocketMessageHandler.SendConnectionType(ConnectionTypes.Client);
            }
        }
        public static void Connect(string sessionIDToUse)
        {
            ConnectionType = ConnectionTypes.ElevatedClient;
            if (InitConnection())
            {
                SessionID = sessionIDToUse;
                //SocketMessageHandler.SendConnectionType(ConnectionTypes.ElevatedClient, sessionIDToUse);
            }
        }
        private static bool InitConnection()
        {
            if (IsConnected)
            {
                if (Config.Current.StartupMode == Config.StartupModes.Notifier)
                {
                    App.Current.Shutdown();
                    return false;
                }
                MessageBox.Show("The client is already connected.", "Already Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            TcpClient = new TcpClient();
            try
            {
                TcpClient.Connect(Config.Current.ClientHost, Config.Current.ClientPort);
                TcpClient.Client.ReceiveBufferSize = Config.Current.BufferSize;
                TcpClient.Client.SendBufferSize = Config.Current.BufferSize;
                SocketMessageHandler = new ClientSocketMessages(TcpClient.Client);
                WaitForServerMessage();
                if (Config.Current.IsClipboardShared)
                {
                    ClipboardManager.Current.BeginWatching(SocketMessageHandler);
                }
                return true;
            }
            catch
            {
                if (Config.Current.StartupMode == Config.StartupModes.Notifier)
                {
                    App.Current.Shutdown();
                    return false;
                }
                MessageBox.Show("Unable to connect.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Pages.Client.Current.RefreshUICall();
                return false;
            }
        }
        private static void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                e.Completed -= ReceiveFromServerCompleted;
                (e as SocketArgs).IsInUse = false;
                Utilities.WriteToLog($"Socket closed in AditClient: {e.SocketError.ToString()}");
                if (Config.Current.StartupMode == Config.StartupModes.Notifier)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        App.Current.Shutdown();
                    });
                    return;
                }
                SessionID = String.Empty;
                Pages.Client.Current.RefreshUICall();
                return;
            }
            SocketMessageHandler.ProcessSocketArgs(e, ReceiveFromServerCompleted, TcpClient.Client);
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
            }
            Pages.Client.Current.RefreshUICall();
        }
    }
}
