using Adit.Code.Client;
using Adit.Controls;
using Adit.Pages;
using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;
using Adit.Code.Server;

namespace Adit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Current { get; private set; }
        public MainWindow()
        {
            InitializeComponent();
            Current = this;

        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TrayIcon.Icon?.ShowCustomBalloon(new ClosedToTrayBalloon(), PopupAnimation.Fade, 5000);
            Config.Save();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initializer.SetGlobalErrorHandler();
            Initializer.SetShutdownMode();
            Config.Load();
            Initializer.ProcessCommandLineArgs(Environment.GetCommandLineArgs());
            TrayIcon.Create();

            ProcessConfiguration();
        }

        private void ProcessConfiguration()
        {
            if (Config.Current.IsClientOnly)
            {
                welcomeToggle.Visibility = Visibility.Collapsed;
                serverToggle.Visibility = Visibility.Collapsed;
                Config.Current.StartupTab = Config.StartupTabs.Client;
            }
            if (Config.Current.IsViewerHidden)
            {
                viewerToggle.Visibility = Visibility.Collapsed;
            }
            if (Config.Current.IsAutoConnectEnabled)
            {
                // TODO
            }

            switch (Config.Current.StartupTab)
            {
                case Config.StartupTabs.Welcome:
                    welcomeToggle.IsChecked = true;
                    mainFrame.Navigate(new Welcome());
                    break;
                case Config.StartupTabs.Client:
                    clientToggle.IsChecked = true;
                    mainFrame.Navigate(new Client());
                    break;
                case Config.StartupTabs.Server:
                    serverToggle.IsChecked = true;
                    mainFrame.Navigate(new Server());
                    break;
                case Config.StartupTabs.Viewer:
                    viewerToggle.IsChecked = true;
                    mainFrame.Navigate(new Pages.Viewer());
                    break;
                default:
                    break;
            }
            switch (Config.Current.StartupMode)
            {
                case Config.StartupModes.Normal:
                    break;
                case Config.StartupModes.Notifier:
                    mainFrame.Navigate(new Notifier());
                    break;
                case Config.StartupModes.Background:
                    this.Close();
                    break;
                default:
                    break;
            }
        }

        private void Welcome_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new Welcome());
        }

        private void Server_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new Server());
        }
        private void Client_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new Client());
        }
        private void Viewer_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new Pages.Viewer());
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new Options());
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            ToggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new About());
        }
        private void ToggleButtonClicked(ToggleButton sender)
        {
            foreach (ToggleButton button in sideMenuStack.Children)
            {
                button.IsChecked = false;
            }
            sender.IsChecked = true;
            Config.Save();
        }

    }
}
