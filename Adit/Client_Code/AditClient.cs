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
        public static void Send(dynamic jsonData)
        {
            string jsonRequest = Utilities.JSON.Serialize(jsonData);
            byte[] outBuffer = Encoding.UTF8.GetBytes(jsonRequest);
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(outBuffer, 0, outBuffer.Length);
            TcpClient.Client.SendAsync(socketArgs);
        }

        public static async Task Connect()
        {
            if (TcpClient?.Connected == true)
            {
                throw new Exception("Client is already connected.");
            }
            TcpClient = new TcpClient();
            try
            {
                await TcpClient.ConnectAsync(Config.Current.ClientHost, Config.Current.ClientPort);
            }
            catch
            {
                MessageBox.Show("Unable to connect.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            ClientMain.Current.RefreshUICall();
            Send(new { Test = "hi" });
        }
    }
}
