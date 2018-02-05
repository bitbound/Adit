using Adit.Models;
using Adit.Pages;
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
        private static int bufferSize = 9999999;
        public static List<ClientConnection> ClientList { get; set; } = new List<ClientConnection>();
        public static List<ClientSession> SessionList { get; set; } = new List<ClientSession>();

        public static void Start()
        {
            if (tcpListener?.Server?.IsBound == true)
            {
                throw new Exception("Server is already running.");
            }
            tcpListener = TcpListener.Create(Config.Current.ServerPort);
            tcpListener.Server.ReceiveBufferSize = bufferSize;
            tcpListener.Server.SendBufferSize = bufferSize;
            tcpListener.Start();
            WaitForClientConnection();
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
                return ClientList.Count;
            }
        }
        private static void AcceptClientCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var clientConnection = new ClientConnection();
                clientConnection.Socket = e.AcceptSocket;
                clientConnection.SocketMessageHandler = new ServerSocketMessages(clientConnection);
                ClientList.Add(clientConnection);
                var session = new ClientSession();
                session.ConnectedClients.Add(clientConnection);
                SessionList.Add(session);
                ServerMain.Current.RefreshUICall();
                WaitForClientMessage(clientConnection);
                WaitForClientConnection();
            }
        }
        private static void WaitForClientConnection()
        {
            var acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += AcceptClientCompleted;
            tcpListener.Server.AcceptAsync(acceptArgs);
        }
        private static void WaitForClientMessage(ClientConnection aditClient)
        {
            var receiveBuffer = new byte[bufferSize];
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            socketArgs.UserToken = aditClient;
            socketArgs.Completed += ReceiveFromClientCompleted;
            aditClient.Socket.ReceiveAsync(socketArgs);
        }

        private static void ReceiveFromClientCompleted(object sender, SocketAsyncEventArgs e)
        {
            if ((e.UserToken as ClientConnection).Socket.Connected == false)
            {
                HandleClientDisconnect(e.UserToken as ClientConnection);
                return;
            }
            if (e.SocketError != SocketError.Success)
            {
                Utilities.WriteToLog($"Socket error in AditServer: {e.SocketError.ToString()}");
                HandleClientDisconnect(e.UserToken as ClientConnection);
                return;
            }
            var result = (e.UserToken as ClientConnection).SocketMessageHandler.ProcessSocketMessage(e.Buffer);
            if (!result)
            {
                HandleClientDisconnect(e.UserToken as ClientConnection);
                return;
            }
            WaitForClientMessage(e.UserToken as ClientConnection);
        }

        private static void HandleClientDisconnect(ClientConnection connection)
        {
            var session = SessionList.Find(x => x.ConnectedClients.Contains(connection));
            if (session != null)
            {
                session.ConnectedClients.Remove(connection);
                if (session.ConnectedClients.Count == 0)
                {
                    SessionList.Remove(session);
                }
            }
            ClientList.Remove(connection);
            ServerMain.Current.RefreshUICall();
        }

        public static void Stop()
        {
            tcpListener.Stop();
            foreach (var client in ClientList)
            {
                client.Socket.Close();
            }
            ClientList.Clear();
            SessionList.Clear();
            tcpListener.Server.Close();
            ServerMain.Current.RefreshUICall();
        }
    }
}
