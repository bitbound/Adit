using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Code.Shared
{
    public class Config
    {
        public static Config Current { get; set; } = new Config();


        public StartupModes StartupMode { get; set; } = StartupModes.Normal;
        public StartupTabs StartupTab { get; set; } = StartupTabs.Welcome;
        public string ProductName { get; set; } = "Adit";
        public int BufferSize { get; set; } = 50000000;


        public bool IsServerTabVisible { get; set; } = true;
        public bool IsViewerTabVisible { get; set; } = true;
        public bool IsOptionsTabVisible { get; set; } = true;
        public bool IsWelcomeTabVisible { get; set; } = true;
        public bool IsClientTabVisible { get; set; } = true;
        public bool IsHubTabVisible { get; set; } = true;
        public bool IsTargetServerConfigurable { get; set; } = true;


        public string ServerHost { get; set; } = "localhost";
        public int ServerPort { get; set; } = 54765;
        public bool IsServerAutoStartEnabled { get; set; }


        public string ClientHost { get; set; } = "localhost";
        public int ClientPort { get; set; } = 54765;
        public bool IsClientAutoConnectEnabled { get; set; }
        public bool IsUACHandled { get; set; }


        public string ViewerHost { get; set; } = "localhost";
        public int ViewerPort { get; set; } = 54765;
        public bool IsViewerScaleToFit { get; set; } = true;
        public bool IsViewerMaximizedOnConnect { get; set; } = true;
        public bool IsClipboardShared { get; set; } = true;
        public bool IsFollowCursorEnabled { get; set; } = true;

        public string HubHost { get; set; } = "localhost";
        public int HubPort { get; set; } = 54765;
        public string HubKey { get; set; }

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
            Viewer,
            Hub
        }
        public static void Save()
        {
            DirectoryInfo di = Directory.CreateDirectory(Utilities.ProgramFolder);
            File.WriteAllText(Path.Combine(di.FullName, "Config.json"), Utilities.JSON.Serialize(Config.Current));
        }
        public static void Load()
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(Utilities.ProgramFolder, "Config.json"));
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
