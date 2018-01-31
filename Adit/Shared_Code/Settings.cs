using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Shared_Code
{
    public class Settings
    {
        public static Settings Current { get; set; } = new Settings();
        public StartupModes StartupMode { get; set; } = StartupModes.FirstRun;

        public bool IsConfiguredAsClient { get; set; }
        public bool IsViewerAvailable { get; set; }

        public string Configuration
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
            Notifier,
            Hidden
        }
        public static void Load() { }
        public static void Save() { }
    }
}
