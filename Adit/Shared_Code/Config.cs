using Adit.Client_Code;
using Adit.Server_Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Shared_Code
{
    public class Config
    {
        public static Config Current { get; set; } = new Config();
        public StartupModes StartupMode { get; set; } = StartupModes.Welcome;

        public string ServerHost { get; set; } = "localhost";
        public int ServerPort { get; set; } = 54765;


        public string ClientHost { get; set; } = "localhost";
        public int ClientPort { get; set; } = 54765;
        public bool IsClientOnly { get; set; }
        public bool IsViewerHidden { get; set; }
        public bool IsAutoConnect { get; set; }

        public enum StartupModes
        {
            Welcome,
            Client,
            Server,
            Viewer,
            Notifier,
            Hidden
        }
        public static void Save()
        {
            var di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit"));
            File.WriteAllText(Path.Combine(di.FullName, "GlobalSettings.json"), Utilities.JSON.Serialize(Config.Current));
        }
        public static void Load()
        {
            var fileInfo = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit", "GlobalSettings.json"));
            if (fileInfo.Exists)
            {
                var settings = Utilities.JSON.Deserialize<Config>(File.ReadAllText(fileInfo.FullName));
                foreach (var prop in typeof(Config).GetProperties(BindingFlags.SetProperty))
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
