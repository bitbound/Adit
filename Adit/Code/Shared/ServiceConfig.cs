using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            try
            {
                File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Adit_Service.exe"), Properties.Resources.Adit_Service);
            }
            catch
            {
                MessageBox.Show("Failed to unpack the service into the temp directory.  Try clearing the temp directory.", "Write Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var psi = new ProcessStartInfo(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Adit_Service.exe"), "-install");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            var proc = Process.Start(psi);
            proc.WaitForExit();
            Pages.Options.Current.RefreshUI();
            if (proc.ExitCode == 0)
            {
                MessageBox.Show("Service installation successful.  If necessary, remember to configure access levels in the Computer Hub.  Otherwise, only admins will have access to this computer.", "Install Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Utilities.WriteToLog("Service installation failed with exit code " + proc.ExitCode.ToString());
                MessageBox.Show("Service installation failed.  Please try again or contact the developer for support.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
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
                Pages.Options.Current.RefreshUI();
                MessageBox.Show("Service removal successful.", "Removal Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Pages.Options.Current.RefreshUI();
                Utilities.WriteToLog(ex);
                MessageBox.Show("Service removal failed.  Please try again or contact the developer for support.", "Removal Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static void StartService()
        {

        }
        public static void StopService()
        {

        }
    }
}
