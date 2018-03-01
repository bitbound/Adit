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
            TcpClient.ReceiveBufferSize = Config.Current.BufferSize;
            TcpClient.SendBufferSize = Config.Current.BufferSize;
            try
            {
                TcpClient.Connect(Config.Current.ClientHost, Config.Current.ClientPort);
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


        private static void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                e.Completed -= ReceiveFromServerCompleted;
                (e as SocketArgs).IsInUse = false;
                Utilities.WriteToLog($"Socket closed in AditService: {e.SocketError.ToString()}");
                SessionID = String.Empty;
                return;
            }
            SocketMessageHandler.ProcessSocketArgs(e, ReceiveFromServerCompleted, TcpClient.Client);
        }
    }
}
