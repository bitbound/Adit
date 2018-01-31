using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Server_Code
{
    public class ServerSettings
    {
        public static ServerSettings Current { get; set; } = new ServerSettings();
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 54765;

        public static void Save()
        {
            var di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit"));
            File.WriteAllText(Path.Combine(di.FullName, "ServerSettings.json"), Utilities.JSON.Serialize(ServerSettings.Current));
        }
        public static void Load()
        {
            var fileInfo = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit", "ServerSettings.json"));
            if (fileInfo.Exists)
            {
                using (var fs = new FileStream(fileInfo.FullName, System.IO.FileMode.OpenOrCreate))
                {
                    var settings = Utilities.JSON.Deserialize<ServerSettings>(File.ReadAllText(fileInfo.FullName));
                    foreach (var prop in typeof(ServerSettings).GetProperties())
                    {
                        prop.SetValue(ServerSettings.Current, prop.GetValue(settings));
                    }
                }
            }
        }
    }
}
