using Adit.Client_Code;
using Adit.Shared_Code;
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
    public partial class ClientMain : Page
    {
        public static ClientMain Current { get; set; }

        public ClientMain()
        {
            InitializeComponent();
            Current = this;
            Initializer.CleanupTempFiles();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
        }
        private void TextSessionID_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Clipboard.SetText(textSessionID.Text);
            textSessionID.SelectAll();
            Utilities.ShowToolTip(textSessionID, "Copied to clipboard!", Colors.Green);

        }
        private void ButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            buttonMenu.ContextMenu.IsOpen = !buttonMenu.ContextMenu.IsOpen;
        }

        private void TextAgentStatus_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (capturing)
            //{
            //    capturing = false;
            //    Socket.Dispose();
            //    stackMain.Visibility = Visibility.Collapsed;
            //    stackReconnect.Visibility = Visibility.Visible;
            //}
        }

        private void TextFilesTransferred_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var di = new DirectoryInfo(System.IO.Path.GetTempPath() + @"\Adit");
            if (di.Exists)
            {
                Process.Start("explorer.exe", di.FullName);
            }
            else
            {
                Utilities.ShowToolTip(textFilesTransferred, "No files available.", Colors.Black);
            }
        }
        private void UpgradeToService(object sender, RoutedEventArgs e)
        {
            //var services = System.ServiceProcess.ServiceController.GetServices();
            //var aditService = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
            //if (aditService != null)
            //{
            //    System.Windows.MessageBox.Show("The Adit Service is already installed.  Please connect via Unattended Mode from the remote control.", "Service Already Installed", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            //if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            //{
            //    System.Windows.MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to upgrade to a service.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            //try
            //{
            //    File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Adit_Service.exe"), Properties.Resources.Adit_Service);
            //}
            //catch
            //{
            //    System.Windows.MessageBox.Show("Failed to unpack the service into the temp directory.  Try clearing the temp directory.", "Write Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}
            //var psi = new ProcessStartInfo(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Adit_Service.exe"), "-install -once");
            //psi.WindowStyle = ProcessWindowStyle.Hidden;
            //Process.Start(psi);
            //AditClient.Send(new
            //{
            //    Type = "ConnectUpgrade",
            //    ComputerName = Environment.MachineName
            //});
        }

        private void MenuUnattended_Click(object sender, RoutedEventArgs e)
        {

            if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            {
                System.Windows.MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to access unattended features.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //new UnattendedWindow().ShowDialog();
        }
        private void MenuUAC_Click(object sender, RoutedEventArgs e)
        {
            //handleUAC = menuUAC.IsChecked;
        }
      
        private async void ConnectButtonClicked(object sender, RoutedEventArgs e)
        {
            stackConnect.Visibility = Visibility.Collapsed;
            stackMain.Visibility = Visibility.Visible;
            await AditClient.Connect(Models.ConnectionTypes.Client);
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (AditClient.TcpClient?.Client?.Connected == true)
            {
                AditClient.TcpClient.Close();
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            textHost.Text = Config.Current.ClientHost;
            textPort.Text = Config.Current.ClientPort.ToString();
            textSessionID.Text = AditClient.SessionID;
            buttonPartnersConnected.Text = AditClient.PartnersConnected.ToString();
            if (AditClient.TcpClient?.Client?.Connected == true)
            {
                stackConnect.Visibility = Visibility.Collapsed;
                stackMain.Visibility = Visibility.Visible;
            }
            else
            {
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
