using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Adit_Service
{
    class Program
    {
        static void Main()
        {
            var args = Environment.GetCommandLineArgs().ToList();
            // If "-interactive" switch present, run service as an interactive console app.
            if (args.Exists(str => str.ToLower() == "-interactive"))
            {
                AditService.Connect();
            }
            else if (args.Exists(str => str.ToLower() == "-install"))
            {
                InstallService(args);
            }
            else if (args.Exists(str => str.ToLower() == "-uninstall"))
            {
                UninstallService();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new WindowsService()
                };
                ServiceBase.Run(ServicesToRun);
//#if DEBUG
//                AditService.Connect();
//#else
//                ServiceBase[] ServicesToRun;
//                ServicesToRun = new ServiceBase[]
//                {
//                    new WindowsService()
//                };
//                ServiceBase.Run(ServicesToRun);
//#endif
            }
            while (true)
            {
                System.Threading.Thread.Sleep(60000);
            }
        }

        private static void InstallService(List<string>  args)
        {
            try
            {
                var di = Directory.CreateDirectory(Utilities.ProgramFolder);
                var installPath = Path.Combine(di.FullName, Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                var serv = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == "Adit_Service");
                if (serv == null)
                {
                    string[] command;
                    if (args.Exists(str => str.ToLower() == "-once"))
                    {
                        command = new String[] { "/assemblypath=\"" + installPath + "\" -once" };
                    }
                    else
                    {
                        command = new String[] { "/assemblypath=" + installPath };
                    }
                    ServiceInstaller ServiceInstallerObj = new ServiceInstaller();
                    InstallContext Context = new InstallContext("", command);
                    ServiceInstallerObj.Context = Context;
                    ServiceInstallerObj.DisplayName = "Adit Service";
                    ServiceInstallerObj.Description = "Background service that accepts connections for the Adit Client.";
                    ServiceInstallerObj.ServiceName = "Adit_Service";
                    ServiceInstallerObj.StartType = ServiceStartMode.Automatic;
                    ServiceInstallerObj.DelayedAutoStart = true;
                    ServiceInstallerObj.Parent = new ServiceProcessInstaller();

                    System.Collections.Specialized.ListDictionary state = new System.Collections.Specialized.ListDictionary();
                    ServiceInstallerObj.Install(state);
                }
                serv = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == "Adit_Service");
                if (serv != null && serv.Status != ServiceControllerStatus.Running)
                {
                    serv.Start();
                }
                var psi = new ProcessStartInfo("cmd.exe", "/c sc.exe failure \"Adit_Service\" reset=5 actions=restart/5000");
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                Process.Start(psi).WaitForExit();

                // Set Secure Attention Sequence policy to allow app to simulate Ctrl + Alt + Del.
                var subkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                subkey.SetValue("SoftwareSASGeneration", "3", Microsoft.Win32.RegistryValueKind.DWord);
                Utilities.WriteToLog("Install completed.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                Environment.Exit(1);
            }
        }
        private static void UninstallService()
        {
            try
            {
                Utilities.WriteToLog("Uninstall initiated.");
                Process.Start("cmd.exe", "/c sc delete Adit_Service").WaitForExit();
                var procs = Process.GetProcessesByName("Adit_Service").Where(proc => proc.Id != Process.GetCurrentProcess().Id);
                foreach (var proc in procs)
                {
                    proc.Kill();
                }

                // Remove Secure Attention Sequence policy to allow app to simulate Ctrl + Alt + Del.
                var subkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true);
                if (subkey.GetValue("SoftwareSASGeneration") != null)
                {
                    subkey.DeleteValue("SoftwareSASGeneration");
                }
                Utilities.WriteToLog("Uninstall completed.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                Environment.Exit(1);
            }
        }
    }
}
