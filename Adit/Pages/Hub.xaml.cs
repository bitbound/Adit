using Adit.Code.Server;
using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Adit.Pages
{
    /// <summary>
    /// Interaction logic for Hub.xaml
    /// </summary>
    public partial class Hub : Page
    {
        public static Hub Current { get; set; }
        public static  byte[] ReceiveBuffer { get; set; } = new byte[Config.Current.BufferSize];
        private TcpClient client;
        private string currentHost = Config.Current.ServerHost;
        private int currentPort = Config.Current.ServerPort;
        public Hub()
        {
            InitializeComponent();
            Current = this;
            ComputerHub.Current.Load();
        }

        private void ConnectToClient_Click(object sender, RoutedEventArgs e)
        {
            if (datagridComputers.SelectedItem == null)
            {
                return;
            }
            Config.Current.ViewerHost = currentHost;
            Config.Current.ViewerPort = currentPort;
            MainWindow.Current.hubToggle.IsChecked = false;
            MainWindow.Current.viewerToggle.IsChecked = true;
            MainWindow.Current.mainFrame.Navigate(new Pages.Viewer((datagridComputers.SelectedItem as HubComputer).SessionID));
        }

        private void ViewRemoteServer_Click(object sender, RoutedEventArgs e)
        {
            connectionsGrid.Visibility = Visibility.Collapsed;
            remoteServerConnectStack.Visibility = Visibility.Visible;
        }

        private void CancelServerConnect_Click(object sender, RoutedEventArgs e)
        {
            remoteServerConnectStack.Visibility = Visibility.Collapsed;
            connectionsGrid.Visibility = Visibility.Visible;
        }

        private async void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new TcpClient();
                Utilities.ShowToolTip(this, System.Windows.Controls.Primitives.PlacementMode.Center, "Attempting to connect...");
                await client.ConnectAsync(Config.Current.HubHost, Config.Current.HubPort);
            }
            catch
            {
                MessageBox.Show("Unable to connect to server.", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var request = new
            {
                Type = "HubDataRequest",
                Key = Config.Current.HubKey
            };
            client.Client.Send(Encoding.UTF8.GetBytes(Utilities.JSON.Serialize(request)));
            var startWait = DateTime.Now;
            do
            {
                client.Client.Poll(1, SelectMode.SelectRead);
                await Task.Delay(500);
                if (DateTime.Now - startWait > TimeSpan.FromSeconds(5))
                {
                    MessageBox.Show("Unable to connect.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    client.Close();
                    return;
                }
            }
            while (client.Available == 0);
            var receiveSize = client.Available;
            client.Client.Receive(ReceiveBuffer);
            var response = Utilities.JSON.Deserialize<dynamic>(Encoding.UTF8.GetString(ReceiveBuffer.Take(receiveSize).ToArray()));
            if (response["Status"] != "ok")
            {
                MessageBox.Show(response["Status"], "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                ComputerHub.Current.ComputerList.Clear();
                viewingServer.Text = Config.Current.HubHost;
                currentHost = Config.Current.HubHost;
                currentPort = Config.Current.HubPort;
                var computerList = response["ComputerList"] as object[];
                foreach (Dictionary<string,object> clientObj in computerList)
                {
                    var newClient = new HubComputer()
                    {
                        ComputerName = (string)clientObj["ComputerName"],
                        CurrentUser = (string)clientObj["CurrentUser"],
                        MACAddress = (string)clientObj["MACAddress"],
                        ConnectionType = (ConnectionTypes)clientObj["ConnectionType"],
                        LastReboot = (DateTime?)clientObj["LastReboot"],
                        SessionID = (string)clientObj["SessionID"],
                        Alias = (string)clientObj["Alias"],
                        LastOnline = (DateTime?)clientObj["LastOnline"],
                        IsOnline = (bool)clientObj["IsOnline"]
                    };
                    ComputerHub.Current.ComputerList.Add(newClient);
                }
                datagridComputers.Items.Refresh();
                remoteServerConnectStack.Visibility = Visibility.Collapsed;
                connectionsGrid.Visibility = Visibility.Visible;
                datagridComputers.IsReadOnly = true;
                deleteButton.Visibility = Visibility.Collapsed;
                disconnectButton.Visibility = Visibility.Collapsed;
            }
            client.Close();
        }

        private void DataDridComputers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            datagridComputers.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateSource();
            ComputerHub.Current.Save();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            for (var i = datagridComputers.SelectedItems.Count - 1; i >= 0; i--)
            {
                ComputerHub.Current.ComputerList.Remove((HubComputer)datagridComputers.SelectedItems[i]);
            }
        }

        private void DisconnectClient_Click(object sender, RoutedEventArgs e)
        {
            for (var i = datagridComputers.SelectedItems.Count - 1; i >= 0; i--)
            {
                var computer = (HubComputer)datagridComputers.SelectedItems[i];
                AditServer.ClientList.RemoveAll(x => x.ID == computer?.ID);
                ComputerHub.Current.ComputerList.Remove(computer);
            }
        }

        private void DatagridComputers_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConnectToClient_Click(sender, e);
        }
    }
}
