using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Adit.Shared_Code
{
    public static class TrayIcon
    {
        public static TaskbarIcon Icon { get; set; }
        public static void Create()
        {
            if (Icon?.IsDisposed == false)
            {
                Icon.Dispose();
            }
            Icon = new TaskbarIcon();
            Icon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/icons8-connect-64.ico"));
            CreateContextMenu();
            Icon.TrayMouseDoubleClick += (send, arg) => {
                new MainWindow().Show();
            };
            Icon.TrayRightMouseUp += (send, arg) => {
                if (MainWindow.Current.IsVisible == true)
                {
                    (Icon.ContextMenu.Items[0] as MenuItem).Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    (Icon.ContextMenu.Items[0] as MenuItem).Visibility = System.Windows.Visibility.Visible;
                }
                Icon.ContextMenu.IsOpen = true;
            };
        }

        private static void CreateContextMenu()
        {
            Icon.ContextMenu = new ContextMenu();
            MenuItem item;
            item = new MenuItem() { Header = "Show" };
            item.Click += (send, arg) =>
            {
                Config.Current.StartupMode = Config.StartupModes.Normal;
                new MainWindow().Show();
            };
            Icon.ContextMenu.Items.Add(item);
            item = new MenuItem() { Header = "Open Client" };
            item.Click += (send, arg) =>
            {
                Config.Current.StartupMode = Config.StartupModes.Normal;
                Config.Current.StartupTab = Config.StartupTabs.Client;
                new MainWindow().Show();
            };
            Icon.ContextMenu.Items.Add(item);
            item = new MenuItem() { Header = "Open Viewer" };
            item.Click += (send, arg) =>
            {
                Config.Current.StartupMode = Config.StartupModes.Normal;
                Config.Current.StartupTab = Config.StartupTabs.Viewer;
                new MainWindow().Show();
            };
            Icon.ContextMenu.Items.Add(item);
            item = new MenuItem() { Header = "Open Server" };
            item.Click += (send, arg) =>
            {
                Config.Current.StartupMode = Config.StartupModes.Normal;
                Config.Current.StartupTab = Config.StartupTabs.Server;
                new MainWindow().Show();
            };
            Icon.ContextMenu.Items.Add(item);
            item = new MenuItem() { Header = "Exit" };
            item.Click += (send, arg) =>
            {
                App.Current.Shutdown(0);
            };
            Icon.ContextMenu.Items.Add(item);
        }
    }
}
