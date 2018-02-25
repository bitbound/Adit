using Adit.Code.Client;
using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
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
    /// Interaction logic for ClientMain.xaml
    /// </summary>
    public partial class Client : Page
    {
        public static Client Current { get; set; } = new Client();

        public Client()
        {
            InitializeComponent();
            Current = this;
            Initializer.CleanupTempFiles();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.Current.IsClientAutoConnectEnabled && !AditClient.IsConnected)
            {
                stackConnect.Visibility = Visibility.Collapsed;
                stackMain.Visibility = Visibility.Visible;
                AditClient.Connect();
            }
            RefreshUI();
        }
        private void TextSessionID_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Clipboard.SetText(textSessionID.Text);
            textSessionID.SelectAll();
            Utilities.ShowToolTip(textSessionID, System.Windows.Controls.Primitives.PlacementMode.Right, "Copied to clipboard!");

        }

        private void TextFilesTransferred_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var files = Directory.GetFiles(Utilities.FileTransferFolder);
            if (files.Count() > 0)
            {
                Process.Start("explorer.exe", Utilities.FileTransferFolder);
            }
            else
            {
                Utilities.ShowToolTip(textFilesTransferred, System.Windows.Controls.Primitives.PlacementMode.Right, "No files available.");
            }
        }
      
        private void ConnectButtonClicked(object sender, RoutedEventArgs e)
        {
            stackConnect.Visibility = Visibility.Collapsed;
            stackMain.Visibility = Visibility.Visible;
            AditClient.Connect();
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (AditClient.IsConnected)
            {
                AditClient.TcpClient.Close();
                AditClient.TcpClient.Dispose();
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            textHost.Text = Config.Current.ClientHost;
            textPort.Text = Config.Current.ClientPort.ToString();
            textSessionID.Text = AditClient.SessionID;
            textFilesTransferred.Text = Directory.GetFiles(Utilities.FileTransferFolder).Count().ToString();

            if (AditClient.IsConnected)
            {
                textPartnersConnected.Text = Math.Max(AditClient.ParticipantList.Count - 1, 0).ToString();
                stackConnect.Visibility = Visibility.Collapsed;
                stackMain.Visibility = Visibility.Visible;
            }
            else
            {
                textSessionID.Text = "Retrieving...";
                textPartnersConnected.Text = "0";
                stackMain.Visibility = Visibility.Collapsed;
                stackConnect.Visibility = Visibility.Visible;
            }
            
        }
        // To refresh UI from other threads.
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() => RefreshUI());
        }
    }
}
