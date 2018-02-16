using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Adit_Service
{
    partial class WindowsService : ServiceBase
    {
        public WindowsService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            AditService.Connect();
        }
        protected override void OnStop()
        {
            if (Environment.GetCommandLineArgs().ToList().Exists(str => str.ToLower() == "-once"))
            {
                var thisProc = Process.GetCurrentProcess();
                var allProcs = Process.GetProcessesByName("Adit_Service");
                foreach (var proc in allProcs)
                {
                    if (proc.Id != thisProc.Id)
                    {
                        proc.Kill();
                    }
                }
                Process.Start("cmd", "/c sc delete Adit_Service");
            }
            base.OnStop();
        }
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            AditService.SocketMessageHandler.SendHeartbeat();
        }
    }
}
