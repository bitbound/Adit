using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Shared
{
    public static class ServiceConfig
    {
        public static bool IsServiceInstalled
        {
            get
            {
                var services = System.ServiceProcess.ServiceController.GetServices();
                var aditService = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
                return aditService != null;
            }
        }
        public static bool IsServiceRunning
        {
            get
            {
                if (!IsServiceInstalled)
                {
                    return false;
                }
                var services = System.ServiceProcess.ServiceController.GetServices();
                var aditService = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
                return aditService.Status == System.ServiceProcess.ServiceControllerStatus.Running;
            }
        }
        public static void InstallService()
        {
            if (!Utilities.IsAdministrator)
            {
                MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to install the service.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Utilities.WriteToLog("Install initiated.");
            Process.Start("cmd.exe", "/c sc delete Adit_Service").WaitForExit();

            foreach (var proc in Process.GetProcessesByName("Adit_Service"))
            {
                proc.Kill();
            }

            foreach (var proc in Process.GetProcessesByName("Adit").Where(proc => proc.Id != Process.GetCurrentProcess().Id))
            {
                proc.Kill();
            }

            var serviceEXEPath = Path.Combine(Utilities.ProgramFolder, "Adit_Service.exe");
            if (!File.Exists(serviceEXEPath))
            {
                MessageBox.Show("The Adit Service wasn't found in the installation directory.  Please reinstall Adit.", "Files Missing", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var psi = new ProcessStartInfo(serviceEXEPath, "-install");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            var installProcess = Process.Start(psi);
            installProcess.WaitForExit();
            if (installProcess.ExitCode == 0)
            {
                MessageBox.Show("Service installation successful.", "Install Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                var services = System.ServiceProcess.ServiceController.GetServices();
                var service = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
                Task.Run(() => {
                    service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                    Pages.Options.Current.RefreshUICall();
                });
            }
            else
            {
                Utilities.WriteToLog($"Service installation failed with exit code {installProcess.ExitCode.ToString()}.");
                MessageBox.Show("Service installation failed.  Please try again or contact the developer for support.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Pages.Options.Current.RefreshUI();
            }
        }
        public static void RemoveService()
        {
            try
            {
                var psi = new ProcessStartInfo("cmd", "/c sc delete Adit_Service");
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi);
                var allProcs = Process.GetProcessesByName("Adit_Service");
                foreach (var proc in allProcs)
                {
                    proc.Kill();
                }
                MessageBox.Show("Service removal successful.", "Removal Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                Pages.Options.Current.RefreshUI();
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                MessageBox.Show("Service removal failed.  Please try again or contact the developer for support.", "Removal Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Pages.Options.Current.RefreshUI();
            }
        }
        public static void StartService()
        {
            var services = System.ServiceProcess.ServiceController.GetServices();
            var service = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
            service.Start();

            Task.Run(() => {
                service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                Pages.Options.Current.RefreshUICall();
            });
        }
        public static void StopService()
        {
            var services = System.ServiceProcess.ServiceController.GetServices();
            var service = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
            service.Stop();

            Task.Run(() => {
                service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                Pages.Options.Current.RefreshUICall();
            });
        }
    }
}
