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
        internal static void ParseCommandLineArgs(string[] args)
        {
            
        }

        internal static void CleanupTempFiles()
        {
            // Clean up temp files from previous file transfers.
            var di = new DirectoryInfo(Path.GetTempPath() + @"\Adit");
            if (di.Exists)
            {
                di.Delete(true);
            }
        }
        internal static void SetGlobalErrorHandler()
        {
            App.Current.DispatcherUnhandledException += DispatcherUnhandledException;
        }
        private static void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Utilities.WriteToLog(e.Exception, Settings.Current.StartupMode.ToString());
            System.Windows.MessageBox.Show("There was an error from which Adit couldn't recover.  If the issue persists, please contact the developer.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        internal static void SetShutdownMode()
        {
            App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            App.Current.Exit += (send, arg) =>
            {
                Settings.Save();
                if (TrayIcon.Icon?.IsDisposed == false)
                {
                    TrayIcon.Icon.Dispose();
                }
            };
        }

        internal static void ProcessConfiguration()
        {
            if (string.IsNullOrWhiteSpace(Settings.Current.Configuration))
            {
                return;
            }
            var config = Utilities.JSON.Deserialize<ClientConfiguration>(Settings.Current.Configuration);
            ClientSettings.Current.Host = config.TargetServerHost;
            ClientSettings.Current.Port = config.TargetServerPort;
            MainWindow.Current.ConfigureForClient();
            if (!config.IsViewerAvailable)
            {
                MainWindow.Current.HideViewer();
            }
        }
    }
}
