using Adit.Server_Code;
using Adit.Shared_Code;
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
        public static ServerMain Current { get; set; }
        public ServerMain()
        {
            InitializeComponent();
            Current = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
        }
        private void ButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).ContextMenu.IsOpen = !(sender as Button).ContextMenu.IsOpen;
        }


        private void ImageCreateClientInfo_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Clients must be created from this screen in order to use this server.\r\n\r\nThe connection information is embedded in the client EXE so there is no configuration required for the end user.\r\n\r\nIf you update the host or port, you must create new clients.", "Create Client", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ButtonServerStatus_Click(object sender, RoutedEventArgs e)
        {
            if (Server_Code.AditServer.IsEnabled)
            {
                var result = MessageBox.Show("Your server is currently running.  Are you sure you want to stop it?", "Confirm Shutdown", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
                AditServer.Stop();
                RefreshUI();
            }
            else
            {
                AditServer.Start();
            }
            RefreshUI();
        }


        private void RefreshUI()
        {
            if (AditServer.IsEnabled)
            {
                buttonServerStatus.Content = "Running";
                buttonServerStatus.Background = new SolidColorBrush(Colors.LightSteelBlue);
            }
            else
            {
                buttonServerStatus.Content = "Stopped";
                buttonServerStatus.Background = new SolidColorBrush(Colors.White);
            }
            textHost.Text = Config.Current.ServerHost;
            textPort.Text = Config.Current.ServerPort.ToString();
            buttonConnectedClients.Content = AditServer.ClientCount.ToString();
        }
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() => RefreshUI());
        }
    }
}
