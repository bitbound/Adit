using Adit.Code.Hub;
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
        public Hub()
        {
            InitializeComponent();
            Current = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
            if (Code.Hub.AditHub.Current.IsConnected)
            {
                Code.Hub.AditHub.Current.SocketMessageHandler.SendHubDataRequest();
            }
            else
            {
                Code.Hub.AditHub.Current.Load();
            }
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            datagridComputers.DataContext = null;
        }
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.RefreshUI();
            });
        }
        public void RefreshUI()
        {
            if (Code.Hub.AditHub.Current.IsConnected)
            {
                viewingServer.Text = Config.Current.HubHost;
                hyperDisconnectRemoteServer.Visibility = Visibility.Visible;
                hyperViewRemoteServer.Visibility = Visibility.Collapsed;
            }
            else
            {
                viewingServer.Text = "Local";
                hyperDisconnectRemoteServer.Visibility = Visibility.Collapsed;
                hyperViewRemoteServer.Visibility = Visibility.Visible;
            }
            remoteServerConnectStack.Visibility = Visibility.Collapsed;
            connectionsGrid.Visibility = Visibility.Visible;
        }
        private void ConnectToClient_Click(object sender, RoutedEventArgs e)
        {
            if (datagridComputers.SelectedItem == null)
            {
                return;
            }
            MainWindow.Current.hubToggle.IsChecked = false;
            MainWindow.Current.viewerToggle.IsChecked = true;
            if (AditHub.Current.IsConnected)
            {
                Config.Current.ViewerHost = Config.Current.HubHost;
                Config.Current.ViewerPort = Config.Current.HubPort;
            }
            else
            {
                Config.Current.ViewerHost = Config.Current.ServerHost;
                Config.Current.ViewerPort = Config.Current.ServerPort;
            }
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
            Config.Current.HubHost = textHost.Text;
            if (int.TryParse(textPort.Text, out var port))
            {
                Config.Current.HubPort = port;
            }
            AditHub.Current.HubKey = textKey.Text;
            Utilities.ShowToolTip(this, System.Windows.Controls.Primitives.PlacementMode.Center, "Attempting to connect...");
            await Code.Hub.AditHub.Current.Connect();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Code.Hub.AditHub.Current.IsConnected)
            {
                AditHub.Current.SocketMessageHandler.SendHubDeleteClientRequest(datagridComputers.SelectedItems as System.Collections.IList);
            }
            else
            {
                for (var i = datagridComputers.SelectedItems.Count - 1; i >= 0; i--)
                {
                    Code.Hub.AditHub.Current.ComputerList.Remove((HubComputer)datagridComputers.SelectedItems[i]);
                }
            }
        }

        private void DisconnectClient_Click(object sender, RoutedEventArgs e)
        {
            if (Code.Hub.AditHub.Current.IsConnected)
            {
                AditHub.Current.SocketMessageHandler.SendHubDisconnectClientRequest(datagridComputers.SelectedItems as System.Collections.IList);
            }
            else
            {
                for (var i = datagridComputers.SelectedItems.Count - 1; i >= 0; i--)
                {
                    var computer = (HubComputer)datagridComputers.SelectedItems[i];
                    var client = AditServer.ClientList.Find(x => x.ID == computer?.ID);
                    if (client != null)
                    {
                        AditServer.ClientList.Remove(client);
                        client.Socket.Close();
                    }
                    Code.Hub.AditHub.Current.ComputerList.Remove(computer);
                }
            }
        }

        private void DatagridComputers_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConnectToClient_Click(sender, e);
        }

        private void DisconnectRemoteServer_Click(object sender, RoutedEventArgs e)
        {
            AditHub.Current.Disconnect();
            AditHub.Current.Load();
            RefreshUI();
        }
    }
}
