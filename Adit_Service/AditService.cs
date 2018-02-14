using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adit_Service
{
    class AditService
    {
        private static SocketAsyncEventArgs socketArgs;
        private static byte[] receiveBuffer;
        public static TcpClient TcpClient { get; set; }
        public static ServiceSocketMessages SocketMessageHandler { get; set; }
        private static int bufferSize = 9999999;
        public static bool IsConnected
        {
            get
            {
                return TcpClient?.Client?.Connected == true;
            }
        }
        public static string SessionID { get; set; }

        public static async Task Start()
        {
            throw new NotImplementedException();
        }

        internal static async Task StartInteractive()
        {
            throw new NotImplementedException();
        }

        internal static void Connect()
        {
            throw new NotImplementedException();
        }
    }
}
