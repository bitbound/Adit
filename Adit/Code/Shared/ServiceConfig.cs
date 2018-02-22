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
        public static void InstallService(bool silent)
        {
            if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
            {
                if (!silent)
                {
                    System.Windows.MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to install the service.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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

            var programDir = Directory.CreateDirectory(Utilities.ProgramFolder);
            var clientProgramPath = Path.Combine(programDir.FullName, Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));

            var count = 0;
            while (File.Exists(clientProgramPath))
            {
                try
                {
                    File.Delete(clientProgramPath);
                }
                catch (Exception ex)
                {
                    count++;
                    if (count > 10)
                    {
                        Utilities.WriteToLog(ex);
                        if (!silent)
                        {
                            MessageBox.Show("Service installation failed.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, clientProgramPath, true);

            count = 0;
            while (File.Exists(Path.Combine(programDir.FullName, "Adit_Service.exe")))
            {
                try
                {
                    File.Delete(Path.Combine(programDir.FullName, "Adit_Service.exe"));
                }
                catch (Exception ex)
                {
                    count++;
                    if (count > 10)
                    {
                        Utilities.WriteToLog(ex);
                        if (!silent)
                        {
                            MessageBox.Show("Service installation failed.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            try
            {
                File.WriteAllBytes(Path.Combine(programDir.FullName, "Adit_Service.exe"), Properties.Resources.Adit_Service);
            }
            catch
            {
                if (!silent)
                {
                    MessageBox.Show("Failed to unpack the service into the Program Files directory.", "Write Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }
            var psi = new ProcessStartInfo(Path.Combine(programDir.FullName, "Adit_Service.exe"), "-install");
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            var installProcess = Process.Start(psi);
            installProcess.WaitForExit();
            if (installProcess.ExitCode == 0)
            {
                if (!silent)
                {
                    MessageBox.Show("Service installation successful.", "Install Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    var services = System.ServiceProcess.ServiceController.GetServices();
                    var service = services.ToList().Find(sc => sc.ServiceName == "Adit_Service");
                    Task.Run(() => {
                        service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                        Pages.Options.Current.RefreshUICall();
                    });
                }
            }
            else
            {
                Utilities.WriteToLog("Service installation failed with exit code " + installProcess.ExitCode.ToString());
                if (!silent)
                {
                    MessageBox.Show("Service installation failed.  Please try again or contact the developer for support.", "Install Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    Pages.Options.Current.RefreshUI();
                }
            }
        }
        public static void RemoveService(bool silent)
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
                if (!silent)
                {
                    MessageBox.Show("Service removal successful.", "Removal Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                    Pages.Options.Current.RefreshUI();
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                if (!silent)
                {
                    MessageBox.Show("Service removal failed.  Please try again or contact the developer for support.", "Removal Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    Pages.Options.Current.RefreshUI();
                }
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
