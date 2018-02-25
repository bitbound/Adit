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
        private static SocketAsyncEventArgs acceptConnectionArgs;
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
        private static async void AcceptClientCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var clientConnection = new ClientConnection();
                clientConnection.Socket = e.AcceptSocket;
                clientConnection.SocketMessageHandler = new ServerSocketMessages(clientConnection);
                ClientList.Add(clientConnection);

                var socketArgs = new SocketAsyncEventArgs();
                socketArgs.SetBuffer(new byte[Config.Current.BufferSize], 0, Config.Current.BufferSize);
                socketArgs.UserToken = clientConnection;
                socketArgs.Completed += ReceiveFromClientCompleted;

                Pages.Server.Current?.RefreshUICall();

                WaitForClientConnection();
                WaitForClientMessage(socketArgs);
                await clientConnection.SocketMessageHandler.SendEncryptionStatus();
            }
        }
        private static void WaitForClientConnection()
        {
            if (acceptConnectionArgs == null)
            {
                acceptConnectionArgs = new SocketAsyncEventArgs();
                acceptConnectionArgs.Completed += AcceptClientCompleted;
            }
            else
            {
                acceptConnectionArgs.AcceptSocket = null;
            }
            tcpListener.Server.AcceptAsync(acceptConnectionArgs);
        }
        private static void WaitForClientMessage(SocketAsyncEventArgs e)
        {
            var aditClient = (e.UserToken as ClientConnection);
            var willFireCallback = aditClient.Socket.ReceiveAsync(e);
            if (!willFireCallback)
            {
                ReceiveFromClientCompleted(aditClient.Socket, e);
            }
        }

        private static async void ReceiveFromClientCompleted(object sender, SocketAsyncEventArgs e)
        {
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
            await (e.UserToken as ClientConnection).SocketMessageHandler.ProcessSocketMessage(e);
            WaitForClientMessage(e);
        }

        private static async void HandleClientDisconnect(SocketAsyncEventArgs socketArgs)
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
                    await session.ConnectedClients[0].SocketMessageHandler.SendParticipantList(session);
                }
            }
            ClientList.Remove(connection);
            connection.Close();
            socketArgs.Dispose();
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
