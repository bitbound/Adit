using Adit.Code.Server;
using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.ComputerHub
{
    public class Hub
    {
        public static Hub Current { get; set; } = new Hub();
        public Hub()
        {
            Load();
            ComputerList.CollectionChanged += ComputerList_CollectionChanged;
        }


        public ObservableCollection<HubComputer> ComputerList { get; set; } = new ObservableCollection<HubComputer>();
        public bool IsConnected
        {
            get
            {
                return TcpClient?.Client?.Connected == true;
            }
        }

        public ComputerHubSocketMessages SocketMessageHandler { get; set; }
        public TcpClient TcpClient { get; set; }
        public void AddOrUpdateComputer(ClientConnection connection)
        {
            var onlineComputer = ComputerList.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.MACAddress) && x.MACAddress == connection.MACAddress);
            if (onlineComputer != null)
            {
                onlineComputer.IsOnline = true;
            }
            else
            {
                onlineComputer = new HubComputer()
                {
                    IsOnline = true
                };
                MainWindow.Current.Dispatcher.Invoke(() =>
                {
                    ComputerList.Add(onlineComputer);
                });
            }
            onlineComputer.ID = connection.ID;
            onlineComputer.SessionID = connection.SessionID;
            onlineComputer.ConnectionType = connection.ConnectionType;
            onlineComputer.IsOnline = true;
            onlineComputer.CurrentUser = connection.CurrentUser;
            onlineComputer.ComputerName = connection.ComputerName;
            onlineComputer.LastOnline = DateTime.Now;
            onlineComputer.LastReboot = connection.LastReboot;
            onlineComputer.MACAddress = connection.MACAddress;
        }

        public async Task Connect()
        {
            if (IsConnected)
            {
                MessageBox.Show("The hub is already connected.", "Already Connected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            TcpClient = new TcpClient();
            try
            {
                await TcpClient.ConnectAsync(Config.Current.HubHost, Config.Current.HubPort);
                TcpClient.Client.ReceiveBufferSize = Config.Current.BufferSize;
                TcpClient.Client.SendBufferSize = Config.Current.BufferSize;
                SocketMessageHandler = new ComputerHubSocketMessages(TcpClient.Client);
                WaitForServerMessage();
            }
            catch
            {
                MessageBox.Show("Unable to connect.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Pages.Viewer.Current.RefreshUICall();
                return;
            }
        }

        public void Load()
        {
            var fi = new FileInfo(Path.Combine(Utilities.DataFolder, "Hub.json"));
            if (fi.Exists)
            {
                var savedList = Utilities.JSON.Deserialize<List<HubComputer>>(File.ReadAllText(fi.FullName)).Where(x => !string.IsNullOrWhiteSpace(x.MACAddress));
                foreach (var computer in savedList)
                {
                    if (!ComputerList.Any(x => x.MACAddress == computer.MACAddress))
                    {
                        ComputerList.Add(computer);
                    }
                }
            }
            MergeAditServerClientList();
        }

        public void MergeAditServerClientList()
        {
            foreach (var computer in ComputerList)
            {
                computer.IsOnline = false;
            }
            foreach (var connection in AditServer.ClientList)
            {
                AddOrUpdateComputer(connection);
            }
            for (var i = ComputerList.Count - 1; i >= 0; i--)
            {
                var computer = ComputerList[i];
                if (!computer.IsOnline && string.IsNullOrWhiteSpace(computer.MACAddress))
                {
                    ComputerList.Remove(computer);
                }
            }
        }

        public void Save()
        {
            var di = Directory.CreateDirectory(Utilities.DataFolder);
            var computerList = ComputerList.Where(x => !string.IsNullOrWhiteSpace(x.MACAddress)).ToList();
            for (var i = computerList.Count - 1; i >= 0; i--)
            {
                if (computerList.FindAll(x => x.MACAddress == computerList[i].MACAddress).Count > 1)
                {
                    computerList.RemoveAt(i);
                }
            }

            File.WriteAllText(Path.Combine(di.FullName, "Hub.json"), Utilities.JSON.Serialize(computerList));
        }

        private void ComputerList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        private void ReceiveFromServerCompleted(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= ReceiveFromServerCompleted;
            if (e.SocketError != SocketError.Success)
            {
                Utilities.WriteToLog($"Socket closed in ComputerHub: {e.SocketError.ToString()}");
                Pages.Hub.Current.RefreshUICall();
                return;
            }
            SocketMessageHandler.ProcessSocketMessage(e);
            WaitForServerMessage();
        }

        private void WaitForServerMessage()
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
                Pages.Hub.Current.RefreshUICall();
            }
        }
    }
}
