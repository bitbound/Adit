using Adit.Models;
using Adit.Pages;
using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace Adit.Code.Server
{
    public class AditServer
    {
        private static TcpListener tcpListener;
        public static List<ClientConnection> ClientList { get; set; } = new List<ClientConnection>();
        public static List<ClientSession> SessionList { get; set; } = new List<ClientSession>();
        public static bool IsEnabled
        {
            get
            {
                if (tcpListener == null)
                {
                    return false;
                }
                return tcpListener?.Server?.IsBound == true;
            }
        }

        public static void Start()
        {
            if (IsEnabled)
            {
                throw new Exception("Server is already running.");
            }
            tcpListener = TcpListener.Create(Config.Current.ServerPort);
            tcpListener.Server.ReceiveBufferSize = Config.Current.BufferSize;
            tcpListener.Server.SendBufferSize = Config.Current.BufferSize;
            try
            {
                tcpListener.Start();
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    MessageBox.Show("The port is already in use.", "In Use", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            WaitForClientConnection();
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
            e.Completed -= AcceptClientCompleted;
            if (e.SocketError == SocketError.Success)
            {
                var clientConnection = new ClientConnection();
                clientConnection.Socket = e.AcceptSocket;
                clientConnection.SocketMessageHandler = new ServerSocketMessages(clientConnection);
                ClientList.Add(clientConnection);

                Pages.Server.Current?.RefreshUICall();

                WaitForClientMessage(clientConnection);
                WaitForClientConnection();
                clientConnection.SocketMessageHandler.SendEncryptionStatus();
            }
        }
        private static void WaitForClientConnection()
        {
            var acceptConnectionArgs = new SocketAsyncEventArgs();
            acceptConnectionArgs.Completed += AcceptClientCompleted;
            tcpListener.Server.AcceptAsync(acceptConnectionArgs);
        }
        private static void WaitForClientMessage(ClientConnection clientConnection)
        {
            var socketArgs = SocketArgsPool.GetReceiveArg();
            socketArgs.Completed += ReceiveFromClientCompleted;
            socketArgs.UserToken = clientConnection;
            var willFireCallback = clientConnection.Socket.ReceiveAsync(socketArgs);
            if (!willFireCallback)
            {
                ReceiveFromClientCompleted(clientConnection.Socket, socketArgs);
            }
        }

        private static void ReceiveFromClientCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= ReceiveFromClientCompleted;
            if ((e.UserToken as ClientConnection).Socket.Connected == false || e.BytesTransferred == 0)
            {
                HandleClientDisconnect(e);
                return;
            }
            if (e.SocketError != SocketError.Success)
            {
                Utilities.WriteToLog($"Socket closed in AditServer: {e.SocketError.ToString()}");
                HandleClientDisconnect(e);
                return;
            }
            (e.UserToken as ClientConnection).SocketMessageHandler.ProcessSocketMessage(e);
            WaitForClientMessage(e.UserToken as ClientConnection);
        }

        private static void HandleClientDisconnect(SocketAsyncEventArgs socketArgs)
        {
            var connection = socketArgs.UserToken as ClientConnection;
            var session = SessionList.Find(x => x.ConnectedClients.Contains(connection));
            if (session != null)
            {
                session.ConnectedClients.Remove(connection);
                if (session.ConnectedClients.Count == 0)
                {
                    SessionList.Remove(session);
                }
                else if (session.ConnectedClients.Count == 1 && session.ConnectedClients[0].ConnectionType == ConnectionTypes.ElevatedClient)
                {
                    session.ConnectedClients[0].Socket.Disconnect(false);
                }
                else
                {
                    session.ConnectedClients[0].SocketMessageHandler.SendParticipantList(session);
                }
            }
            ClientList.Remove(connection);
            connection.Close();
            Pages.Server.Current?.RefreshUICall();
        }

        public static void Stop()
        {
            tcpListener.Stop();
            ClientList.Clear();
            SessionList.Clear();
            tcpListener.Server.Close();
            Pages.Server.Current.RefreshUICall();
        }
    }
}
