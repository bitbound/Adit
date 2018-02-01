using Adit.Models;
using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Server_Code
{
    public class AditServer
    {
        private static TcpListener tcpListener;
        private static int bufferSize;
        private static List<ClientConnection> clientList = new List<ClientConnection>();
        public static void Start()
        {
            if (tcpListener?.Server?.IsBound == true)
            {
                throw new Exception("Server is already running.");
            }
            tcpListener = TcpListener.Create(Config.Current.ServerPort);
            bufferSize = tcpListener.Server.ReceiveBufferSize;
            var acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += acceptClientCompleted;
            tcpListener.Start();
            tcpListener.Server.AcceptAsync(acceptArgs);
        }

        public static bool IsEnabled
        {
            get
            {
                if (tcpListener == null)
                {
                    return false;
                }
                return tcpListener.Server.IsBound;
            }
        }
        public static int ClientCount
        {
            get
            {
                return clientList.Count;
            }
        }
        private static void acceptClientCompleted(object sender, SocketAsyncEventArgs e)
        {
            var aditClient = new ClientConnection();
            aditClient.Socket = e.AcceptSocket;
            clientList.Add(aditClient);
            handleClient(aditClient);
        }

        private static void handleClient(ClientConnection aditClient)
        {
            var receiveBuffer = new byte[bufferSize];
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            socketArgs.UserToken = aditClient;
            socketArgs.Completed += receiveFromClientCompleted;
            aditClient.Socket.ReceiveAsync(socketArgs);
           
        }

        public static void Stop()
        {
            throw new NotImplementedException();
        }

        private static void receiveFromClientCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                clientList.Remove(e.UserToken as ClientConnection);
                return;
            }
            SocketMessageHandler.ProcessSocketMessage(e.Buffer);
            handleClient(e.UserToken as ClientConnection);
        }
    }
}
