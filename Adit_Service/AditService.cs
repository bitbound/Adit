using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Adit_Service
{
    class AditService
    {
        public static TcpClient TcpClient { get; set; }
        public static ServiceSocketMessages SocketMessageHandler { get; set; }
        public static Timer HeartbeatTimer { get; set; } = new Timer();
        public static bool IsConnected
        {
            get
            {
                return TcpClient?.Client?.Connected == true;
            }
        }
        public static string SessionID { get; set; }

        public static void Connect()
        {
            if (IsConnected)
            {
                return;
            }
            TcpClient = new TcpClient();
            try
            {
                TcpClient.Connect(Config.Current.ClientHost, Config.Current.ClientPort);
                TcpClient.Client.ReceiveBufferSize = Config.Current.BufferSize;
                TcpClient.Client.SendBufferSize = Config.Current.BufferSize;
                SocketMessageHandler = new ServiceSocketMessages(TcpClient.Client);
                WaitForServerMessage();
                SocketMessageHandler.SendHeartbeat();
            }
            catch
            {
                WaitToRetryConnection();
                return;
            }
           
        }

        public static void WaitToRetryConnection()
        {
            var timer = new Timer();
            timer.AutoReset = false;
            timer.Interval = 30000;
            timer.Elapsed += (sender, args) =>
            {
                Utilities.WriteToLog("Failed to connect.");
                Connect();
            };
            timer.Start();
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
        }


        private static async void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= ReceiveFromServerCompleted;
            if (e.SocketError != SocketError.Success)
            {
                Utilities.WriteToLog($"Socket closed in AditService: {e.SocketError.ToString()}");
                SessionID = String.Empty;
                return;
            }
            await SocketMessageHandler.ProcessSocketMessage(e);
            WaitForServerMessage();
        }
    }
}
