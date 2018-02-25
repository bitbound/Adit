using Adit.Models;
using Adit.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Shared
{
    public static class Initializer
    {
        public static bool IsFirstLoad { get; set; } = true;
        public static async Task ProcessCommandLineArgs(string[] args)
        {
            if (args.Contains("-upgrade"))
            {
                var index = args.ToList().IndexOf("-upgrade") + 1;
                await Adit.Code.Client.AditClient.Connect(args[index]);
                Config.Current.StartupMode = Config.StartupModes.Notifier;
            }
            else if (args.Contains("-background"))
            {
                Config.Current.StartupMode = Config.StartupModes.Background;
            }
            else
            {
                Config.Current.StartupMode = Config.StartupModes.Normal;
            }
        }

        public static void CleanupTempFiles()
        {
            // Clean up temp files from previous file transfers.
            var di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "AditFiles"));
            foreach (var file in di.GetFiles())
            {
                if (DateTime.Now - file.LastWriteTime > TimeSpan.FromDays(1))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        Utilities.WriteToLog(ex);
                    }
                }
            }
        }
        public static void SetGlobalErrorHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            App.Current.DispatcherUnhandledException += DispatcherUnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utilities.WriteToLog(e.ExceptionObject as Exception);
            Utilities.DisplayErrorMessage();
        }

        private static void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Utilities.WriteToLog(e.Exception);
            Utilities.DisplayErrorMessage();
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
    }
}
