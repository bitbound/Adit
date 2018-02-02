using Adit.Client_Code;
using Adit.Models;
using Adit.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Shared_Code
{
    public static class Initializer
    {
        public static void ProcessCommandLineArgs(string[] args)
        {
            if (args.Contains("-notifier"))
            {
                Config.Current.StartupMode = Config.StartupModes.Notifier;
            }
        }

        public static void CleanupTempFiles()
        {
            // Clean up temp files from previous file transfers.
            var di = new DirectoryInfo(Path.GetTempPath() + @"\Adit");
            if (di.Exists)
            {
                di.Delete(true);
            }
        }
        public static void SetGlobalErrorHandler()
        {
            App.Current.DispatcherUnhandledException += DispatcherUnhandledException;
        }
        private static void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Utilities.WriteToLog(e.Exception, Config.Current.StartupMode.ToString());
            System.Windows.MessageBox.Show("There was an error from which Adit couldn't recover.  If the issue persists, please contact the developer.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        public static void SetShutdownMode()
        {
            App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            App.Current.Exit += (send, arg) =>
            {
                Config.Save();
                if (TrayIcon.Icon?.IsDisposed == false)
                {
                    TrayIcon.Icon.Dispose();
                }
            };
        }

        public static void ProcessConfiguration()
        {
            if (Config.Current.IsClientOnly)
            {
                MainWindow.Current.ConfigureUIForClient();
                Config.Current.StartupMode = Config.StartupModes.Client;
            }
            if (Config.Current.IsViewerHidden)
            {
                MainWindow.Current.HideViewer();
            }
            if (Config.Current.IsAutoConnectEnabled)
            {
                // TODO
            }
        }
    }
}
