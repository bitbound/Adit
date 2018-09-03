using Adit.Code.Server;
using Adit.Code.Shared;
using Adit.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for ServerMain.xaml
    /// </summary>
    public partial class Server : Page
    {
        public static Server Current { get; set; }
        public Server()
        {
            InitializeComponent();
            Current = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
            CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetAsync("https://lucency.co/Services/VersionCheck/?Path=/Downloads/Adit.exe");
                var strVersion = await response.Content.ReadAsStringAsync();
                var serverVersion = Version.Parse(strVersion);

                var fileVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var currentVersion = Version.Parse(fileVersion.FileVersion);

                if (serverVersion > currentVersion)
                {
                    gridUpdate.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }

        private void ButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsOpen = !(sender as Button).ContextMenu.IsOpen;
        }


        private void ImageCreateClientInfo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Clients must be created from this screen in order to use this server.\r\n\r\nThe connection information is embedded in the client EXE so there is no configuration required for the end user.\r\n\r\nIf you update the host or port, you must create new clients.", "Create Client", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void ToggleServerStatus_Click(object sender, MouseButtonEventArgs e)
        {
            if (Code.Server.AditServer.IsEnabled)
            {
                e.Handled = true;
                var result = MessageBox.Show("Your server is currently running.  Are you sure you want to stop it?", "Confirm Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
                AditServer.Stop();
                toggleServerStatus.IsOn = false;
            }
            else
            {
                AditServer.Start();
            }
        }

        private void RefreshUI()
        {
            toggleServerStatus.IsOn = AditServer.IsEnabled;
            toggleEncryption.IsOn = Config.Current.IsEncryptionEnabled;
            textHost.Text = Config.Current.ServerHost;
            textPort.Text = Config.Current.ServerPort.ToString();
            buttonConnectedClients.Content = AditServer.ClientCount.ToString();
        }
        // To refresh UI from other threads.
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() => RefreshUI());
        }

        private void AuthenticationKeys_Click(object sender, RoutedEventArgs e)
        {
            var win = new Windows.AuthenticationKeys();
            win.Owner = MainWindow.Current;
            win.ShowDialog();
        }

        private void ConnectedClients_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Current.mainFrame.Navigate(new Pages.Hub());
        }

        private void ToggleEncryption_Click(object sender, MouseButtonEventArgs e)
        {
            var keyPath = System.IO.Path.Combine(Utilities.DataFolder, "ServerKey");
            if (!System.IO.File.Exists(keyPath))
            {
                var result = MessageBox.Show("No encryption key was found.  Do you want to create one?", "Create New Key", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Encryption.CreateNewKey();
                    Config.Current.IsEncryptionEnabled = true;
                    toggleEncryption.IsOn = true;
                    Config.Save();
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (Encryption.GetServerKey() != null)
                {
                    Config.Current.IsEncryptionEnabled = !toggleEncryption.IsOn;
                    Config.Save();
                }
                else
                {
                    toggleEncryption.IsOn = false;
                }
            }
            
        }

        private void ExportKey_Click(object sender, RoutedEventArgs e)
        {
            var keyPath = System.IO.Path.Combine(Utilities.DataFolder, "ServerKey");
            if (!System.IO.File.Exists(keyPath))
            {
                var result = MessageBox.Show("No encryption key was found.  Do you want to create one?", "Create New Key", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Encryption.CreateNewKey();
                }
                else
                {
                    return;
                }
            }
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderDialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                var keyBytes = Encryption.GetServerKey();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folderDialog.SelectedPath, "ClientKey"), keyBytes);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folderDialog.SelectedPath, "ServiceKey"), keyBytes);
                MessageBox.Show("The encryption key has been exported to a files named \"ClientKey\" and \"ServiceKey\".  Do not rename the files, or they will not work on clients.  Only transfer the keys through a secure connection.\r\n\r\nThe files must be placed in the installation folder on the client (%ProgramData%\\Adit\\ by default).  They will be encrypted on the client device upon first use.", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CreateClientConfig_Click(object sender, RoutedEventArgs e)
        {
            var config = new Config();
            foreach (var prop in typeof(Config).GetProperties())
            {
                var currentVal = prop.GetValue(Config.Current);
                prop.SetValue(config, currentVal);
            }
            config.IsWelcomeTabVisible = false;
            config.IsServerTabVisible = false;
            config.IsHubTabVisible = false;
            config.IsOptionsTabVisible = false;
            config.IsViewerTabVisible = false;
            config.IsServerAutoStartEnabled = false;
            config.IsTargetServerConfigurable = false;
            config.IsClientAutoConnectEnabled = true;
            config.ClientHost = config.ServerHost;
            config.ClientPort = config.ServerPort;
            config.ServiceHost = config.ServerHost;
            config.ServicePort = config.ServerPort;
            config.ViewerHost = config.ServerHost;
            config.ViewerPort = config.ServerPort;
            config.StartupMode = Config.StartupModes.Normal;
            config.StartupTab = Config.StartupTabs.Client;
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.RootFolder = Environment.SpecialFolder.Desktop;
            folderDialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(folderDialog.SelectedPath, "Config.json"), Utilities.JSON.Serialize(config));
                MessageBox.Show("A client configuration has been exported to a file named \"Config.json\".  Do not rename the file, or it will not work on clients.  The config file must be placed in the installation folder on the client (%ProgramData%\\Adit\\ by default).", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Update_Click(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://lucency.co/?Downloads&app=Adit");
        }
    }
}
