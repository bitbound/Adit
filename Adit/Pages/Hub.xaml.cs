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
        public Hub()
        {
            InitializeComponent();
            Current = this;
            ComputerHub.Current.Load();
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
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
            try
            {
                client = new TcpClient();
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
            do
            {
                client.Client.Poll(1, SelectMode.SelectRead);
                await Task.Delay(500);
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
                var computerList = response["ComputerList"] as object[];
                foreach (Dictionary<string,object> clientObj in computerList)
                {
                    var newClient = new HubComputer()
                    {
                        ComputerName = (string)clientObj["ComputerName"],
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
            }
            client.Close();
        }

        private void DataDridComputers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            ComputerHub.Current.Save();
        }
    }
}
