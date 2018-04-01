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
    /// Interaction logic for OptionsMain.xaml
    /// </summary>
    public partial class Options : Page
    {
        public static Options Current { get; set; }
        public Options()
        {
            InitializeComponent();
            Current = this;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshUI();
        }

        private void ServiceInstalled_Click(object sender, MouseButtonEventArgs e)
        {
            if (!Utilities.IsAdministrator)
            {
                MessageBox.Show("Adit must be running as an administrator (i.e. elevated) in order to configure the service.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!ServiceConfig.IsServiceInstalled)
            {
                Config.Current.ServiceHost = textServiceHost.Text;
                if (int.TryParse(textServicePort.Text, out var port))
                {
                    Config.Current.ServicePort = port;
                }
                ServiceConfig.InstallService();
            }
            else
            {
                ServiceConfig.RemoveService();
            }
        }

        private void ServiceRunning_Click(object sender, MouseButtonEventArgs e)
        {
            if (!ServiceConfig.IsServiceInstalled)
            {
                MessageBox.Show("The service must be installed before you can run it.", "Service Not Installed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (ServiceConfig.IsServiceRunning)
            {
                ServiceConfig.StopService();
            }
            else
            {
                ServiceConfig.StartService();
            }
        }

        public void RefreshUI()
        {
            toggleMaximizeViewer.IsOn = Config.Current.IsViewerMaximizedOnConnect;
            toggleScaleToFitViewer.IsOn = Config.Current.IsViewerScaleToFit;
            toggleStartServerAutomatically.IsOn = Config.Current.IsServerAutoStartEnabled;
            toggleServiceInstalled.IsOn = ServiceConfig.IsServiceInstalled;
            toggleServiceRunning.IsOn = ServiceConfig.IsServiceRunning;
            toggleIsWelcomeVisible.IsOn = Config.Current.IsWelcomeTabVisible;
            toggleIsServerVisible.IsOn = Config.Current.IsServerTabVisible;
            toggleIsClientVisible.IsOn = Config.Current.IsClientTabVisible;
            toggleIsViewerVisible.IsOn = Config.Current.IsViewerTabVisible;
            toggleIsOptionsVisible.IsOn = Config.Current.IsOptionsTabVisible;
            toggleIsHugVisible.IsOn = Config.Current.IsHubTabVisible;
            toggleChangeServer.IsOn = Config.Current.IsTargetServerConfigurable;
            toggleScreenFollowsCursors.IsOn = Config.Current.IsFollowCursorEnabled;
            toggleShareClipboard.IsOn = Config.Current.IsClipboardShared;
            toggleClientAutoConnect.IsOn = Config.Current.IsClientAutoConnectEnabled;
        }
        // To refresh UI from other threads.
        public void RefreshUICall()
        {
            this.Dispatcher.Invoke(() => RefreshUI());
        }
        private void MaximizeViewer_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsViewerMaximizedOnConnect = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }

        private void StartServerAutomatically_Click(object sender, MouseButtonEventArgs e)
        {
            if (!Utilities.IsAdministrator)
            {
                MessageBox.Show("Adit must be running as an administrator to change this option.", "Administrator Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Config.Current.IsServerAutoStartEnabled = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
            Utilities.SetStartupRegistry();
        }

        private void ScaleToFitViewer_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsViewerScaleToFit = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }

        private void IsWelcomeVisible_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsWelcomeTabVisible = !(sender as Controls.ToggleSwitch).IsOn;
            MainWindow.Current.welcomeToggle.Visibility = Config.Current.IsWelcomeTabVisible ? Visibility.Visible : Visibility.Collapsed;
            Config.Save();
        }

        private void IsServerVisible_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsServerTabVisible = !(sender as Controls.ToggleSwitch).IsOn;
            MainWindow.Current.serverToggle.Visibility = Config.Current.IsServerTabVisible ? Visibility.Visible : Visibility.Collapsed;
            Config.Save();
        }

        private void IsClientVisible_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsClientTabVisible = !(sender as Controls.ToggleSwitch).IsOn;
            MainWindow.Current.clientToggle.Visibility = Config.Current.IsClientTabVisible ? Visibility.Visible : Visibility.Collapsed;
            Config.Save();
        }

        private void IsViewerVisible_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsViewerTabVisible = !(sender as Controls.ToggleSwitch).IsOn;
            MainWindow.Current.viewerToggle.Visibility = Config.Current.IsViewerTabVisible ? Visibility.Visible : Visibility.Collapsed;
            Config.Save();
        }

        private void IsOptionsVisible_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsOptionsTabVisible = !(sender as Controls.ToggleSwitch).IsOn;
            MainWindow.Current.optionsToggle.Visibility = Config.Current.IsOptionsTabVisible ? Visibility.Visible : Visibility.Collapsed;
            Config.Save();
        }
        private void IsHubVisible_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsHubTabVisible = !(sender as Controls.ToggleSwitch).IsOn;
            MainWindow.Current.hubToggle.Visibility = Config.Current.IsHubTabVisible ? Visibility.Visible : Visibility.Collapsed;
            Config.Save();
        }
        private void CanChangeServer_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsTargetServerConfigurable = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }

        private void FollowCursor_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsFollowCursorEnabled = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }

        private void ShareClipboard_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsClipboardShared = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }

        private void ClientAutoConnect_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsClientAutoConnectEnabled = !(sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }
    }
}
