using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Code.Shared
{
    public class Config
    {
        public static Config Current { get; set; } = new Config();
        public StartupModes StartupMode { get; set; } = StartupModes.Normal;
        public StartupTabs StartupTab { get; set; } = StartupTabs.Welcome;

        public string ServerHost { get; set; } = "localhost";
        public int ServerPort { get; set; } = 54765;


        public string ClientHost { get; set; } = "localhost";
        public int ClientPort { get; set; } = 54765;
        public bool IsClientOnly { get; set; }
        public bool IsTargetServerConfigurable { get; set; } = true;
        public bool IsViewerHidden { get; set; }
        public bool IsAutoConnectEnabled { get; set; }
        public bool IsUACHandled { get; set; }


        public string ViewerHost { get; set; } = "localhost";
        public int ViewerPort { get; set; } = 54765;

        public enum StartupModes
        {
            Normal,
            Notifier,
            Background
        }
        public enum StartupTabs
        {
            Welcome,
            Client,
            Server,
            Viewer
        }
        public static void Save()
        {
            var di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit"));
            File.WriteAllText(Path.Combine(di.FullName, "Config.json"), Utilities.JSON.Serialize(Config.Current));
        }
        public static void Load()
        {
            var fileInfo = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit", "Config.json"));
            if (fileInfo.Exists)
            {
                var settings = Utilities.JSON.Deserialize<Config>(File.ReadAllText(fileInfo.FullName));
                foreach (var prop in typeof(Config).GetProperties())
                {
                    var savedValue = prop.GetValue(settings);
                    if (savedValue != null)
                    {
                        prop.SetValue(Config.Current, prop.GetValue(settings));
                    }
                }
            }
        }
    }
}
