using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private void textSessionID_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //System.Windows.Clipboard.SetText(textSessionID.Text);
            //textSessionID.SelectAll();
            //ShowToolTip(textSessionID, "Copied to clipboard!", Colors.Green);

        }
        private void buttonMenu_Click(object sender, RoutedEventArgs e)
        {
            //buttonMenu.ContextMenu.IsOpen = !buttonMenu.ContextMenu.IsOpen;
        }

        private void textAgentStatus_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (capturing)
            //{
            //    capturing = false;
            //    Socket.Dispose();
            //    stackMain.Visibility = Visibility.Collapsed;
            //    stackReconnect.Visibility = Visibility.Visible;
            //}
        }

        private void textFilesTransferred_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //var di = new DirectoryInfo(Path.GetTempPath() + @"\InstaTech");
            //if (di.Exists)
            //{
            //    Process.Start("explorer.exe", di.FullName);
            //}
            //else
            //{
            //    ShowToolTip(textFilesTransferred, "No files available.", Colors.Black);
            //}
        }
        private async void menuUpgrade_Click(object sender, RoutedEventArgs e)
        {
            //var services = System.ServiceProcess.ServiceController.GetServices();
            //var itService = services.ToList().Find(sc => sc.ServiceName == "InstaTech_Service");
            //if (itService != null)
            //{
            //    System.Windows.MessageBox.Show("The InstaTech Service is already installed.  Please connect via Unattended Mode from the remote control.", "Service Already Installed", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            //if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            //{
            //    System.Windows.MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to upgrade to a service.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            //try
            //{
            //    File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "InstaTech_Service.exe"), Properties.Resources.InstaTech_Service);
            //}
            //catch
            //{
            //    System.Windows.MessageBox.Show("Failed to unpack the service into the temp directory.  Try clearing the temp directory.", "Write Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}
            //var psi = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "InstaTech_Service.exe"), "-install -once");
            //psi.WindowStyle = ProcessWindowStyle.Hidden;
            //Process.Start(psi);
            //await SocketSend(new
            //{
            //    Type = "ConnectUpgrade",
            //    ComputerName = Environment.MachineName
            //});
        }
        private void menuViewer_Click(object sender, RoutedEventArgs e)
        {
            //Process.Start(rcPath);
        }
        private void menuUnattended_Click(object sender, RoutedEventArgs e)
        {

            //if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            //{
            //    System.Windows.MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to access unattended features.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            //new UnattendedWindow().ShowDialog();
        }
        private void menuUAC_Click(object sender, RoutedEventArgs e)
        {
            //handleUAC = menuUAC.IsChecked;
        }
        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            //var win = new AboutWindow();
            //win.Owner = this;
            //win.ShowDialog();
        }
        private void buttonNewSession_Click(object sender, RoutedEventArgs e)
        {
            //stackReconnect.Visibility = Visibility.Collapsed;
            //stackMain.Visibility = Visibility.Visible;
            //InitWebSocket();
        }
    }
}
