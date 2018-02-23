using Adit.Code.Client;
using Adit.Code.Shared;
using Adit.Code.Viewer;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
        public ViewerSurface DrawingSurface { get; private set; }
        public InputHandler InputHandler { get; private set; }

        public Viewer()
        {
            InitializeComponent();
            Current = this;
        }
        public Viewer(string sessionID)
        {
            InitializeComponent();
            Current = this;
            this.Loaded += async (sender, args) =>
            {
                controlsFrame.Visibility = Visibility.Collapsed;
                await AditViewer.Connect(sessionID.Trim());
                RefreshUI();
            };
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Config.Current.IsTargetServerConfigurable)
            {
                stackServerInfo.Visibility = Visibility.Collapsed;
            }
            DrawingSurface = new ViewerSurface();
            InputHandler = new InputHandler(DrawingSurface);
            viewerFrame.Content = DrawingSurface;
            RefreshUI();
            if (AditViewer.IsConnected)
            {
                AditViewer.RequestFullscreen = true;
            }
        }
        public void DrawImageCall(byte[] imageBytes)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    DrawingSurface.DrawImage(imageBytes);
                });
            }
            finally
            {
                AditViewer.SocketMessageHandler.SendImageRequest();
            }
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textSessionID.Text))
            {
                MessageBox.Show("Session ID is required.", "Session ID Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            controlsFrame.Visibility = Visibility.Collapsed;
            await AditViewer.Connect(textSessionID.Text.Trim());
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (AditViewer.IsConnected)
            {
                controlsFrame.Visibility = Visibility.Collapsed;
                viewerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                controlsFrame.Visibility = Visibility.Visible;
                viewerGrid.Visibility = Visibility.Collapsed;
            }

        }
        // To refresh UI from other threads.
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() => RefreshUI());
        }

        private void SendCtrlAltDel_Click(object sender, RoutedEventArgs e)
        {
            AditViewer.SocketMessageHandler.SendCtrlAltDel();
        }

        private void TransferFile_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.ShowDialog();
            if (File.Exists(fileDialog.FileName))
            {
                AditViewer.SocketMessageHandler.SendFileTransfer(fileDialog.FileName);
            }
            Utilities.ShowToolTip(buttonMenu, System.Windows.Controls.Primitives.PlacementMode.Bottom, "File transfer started.");
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            AditViewer.Disconnect();
        }

        private void ViewerMenu_Click(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).ContextMenu.IsOpen = !(sender as FrameworkElement).ContextMenu.IsOpen;
        }

        private void RefreshScreen_Click(object sender, RoutedEventArgs e)
        {
            AditViewer.RequestFullscreen = true;
        }
    }
}
