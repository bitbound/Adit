using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Shared_Code
{
    public static class Settings
    {
        public static StartupModes StartupMode { get; set; } = StartupModes.FirstRun;

        public static void Load() { }
        public static void Save() { }

        public static string Configuration
        {
            get
            {
                using (var ms = Assembly.GetExecutingAssembly().GetManifestResourceStream("Adit.Resources.Config.json"))
                {
                    if (ms == null)
                    {
                        return null;
                    }
                    using (var sr = new StreamReader(ms))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
        public enum StartupModes
        {
            FirstRun,
            Client,
            Server,
            Viewer,
            Notifier
        }
    }
}
