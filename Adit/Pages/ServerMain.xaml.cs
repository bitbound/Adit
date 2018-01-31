using Adit.Server_Code;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public partial class ServerMain : Page
    {
        public ServerMain()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            refreshUI();
        }
        private void buttonMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsOpen = !(sender as Button).ContextMenu.IsOpen;
        }


        private void imageCreateClientInfo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Clients must be created from this screen in order to use this server.\r\n\r\nThe connection information is embedded in the client EXE so there is no configuration required for the end user.\r\n\r\nIf you update the host or port, you must create new clients.", "Create Client", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void buttonServerStatus_Click(object sender, RoutedEventArgs e)
        {
            if (Server_Code.AditServer.IsEnabled)
            {
                var result = MessageBox.Show("Your server is currently running.  Are you sure you want to stop it?", "Confirm Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
                AditServer.Stop();
            }
            AditServer.Start();
            refreshUI();
        }

        private void buttonCreateClient_Click(object sender, RoutedEventArgs e)
        {

        }

        private void refreshUI()
        {
            if (AditServer.IsEnabled)
            {
                buttonServerStatus.Content = "Running";
                buttonServerStatus.IsChecked = true;
            }
            else
            {
                buttonServerStatus.Content = "Stopped";
                buttonServerStatus.IsChecked = false;
            }
            textHost.Text = ServerSettings.Current.Host;
            textPort.Text = ServerSettings.Current.Port.ToString();
            buttonConnectedClients.Content = AditServer.ClientCount.ToString();
        }
    }
}
