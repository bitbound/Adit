using Adit.Code.Client;
using Adit.Code.Shared;
using Adit.Code.Viewer;
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
    /// Interaction logic for ViewerMain.xaml
    /// </summary>
    public partial class Viewer : Page
    {
        public static Viewer Current { get; set; }
        public ViewerSurface Canvas { get; set; }

        public Viewer()
        {
            InitializeComponent();
            Current = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Config.Current.IsAutoConnectEnabled)
            {
                stackServerInfo.Visibility = Visibility.Collapsed;
            }
            Canvas = new ViewerSurface();
            viewerFrame.Children.Add(Canvas);
            RefreshUI();
        }
  
        public void DrawImageCall(byte[] imageBytes)
        {
            this.Dispatcher.Invoke(() => Canvas.DrawImage(imageBytes));
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textSessionID.Text))
            {
                return;
            }
            await AditViewer.Connect(textSessionID.Text.Trim());
        }

        private void RefreshUI()
        {
            if (AditViewer.TcpClient?.Client?.Connected == true)
            {
                controlsFrame.Visibility = Visibility.Collapsed;
            }
            else
            {
                controlsFrame.Visibility = Visibility.Visible;
            }

        }
        // To refresh UI from other threads.
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() => RefreshUI());
        }
    }
}
