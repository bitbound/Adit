using Adit.Code.ComputerHub;
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
            Code.ComputerHub.Hub.Current.Load();
            RefreshUI();
            if (Code.ComputerHub.Hub.Current.IsConnected)
            {
                Code.ComputerHub.Hub.Current.SocketMessageHandler.SendHubDataRequest();
            }
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
            if (Code.ComputerHub.Hub.Current.IsConnected)
            {
                viewingServer.Text = Config.Current.HubHost;
                deleteButton.Visibility = Visibility.Collapsed;
                disconnectButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                viewingServer.Text = "Local";
                deleteButton.Visibility = Visibility.Visible;
                disconnectButton.Visibility = Visibility.Visible;
            }
        }
        private void ConnectToClient_Click(object sender, RoutedEventArgs e)
        {
            if (datagridComputers.SelectedItem == null)
            {
                return;
            }
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
            Utilities.ShowToolTip(this, System.Windows.Controls.Primitives.PlacementMode.Center, "Attempting to connect...");
            await Code.ComputerHub.Hub.Current.Connect();
        }

        private void DataDridComputers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            datagridComputers.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateSource();
            Code.ComputerHub.Hub.Current.Save();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            for (var i = datagridComputers.SelectedItems.Count - 1; i >= 0; i--)
            {
                Code.ComputerHub.Hub.Current.ComputerList.Remove((HubComputer)datagridComputers.SelectedItems[i]);
            }
        }

        private void DisconnectClient_Click(object sender, RoutedEventArgs e)
        {
            for (var i = datagridComputers.SelectedItems.Count - 1; i >= 0; i--)
            {
                var computer = (HubComputer)datagridComputers.SelectedItems[i];
                AditServer.ClientList.RemoveAll(x => x.ID == computer?.ID);
                Code.ComputerHub.Hub.Current.ComputerList.Remove(computer);
            }
        }

        private void DatagridComputers_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConnectToClient_Click(sender, e);
        }
    }
}
