using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace Adit_Service
{
    class Utilities
    {
        public static JavaScriptSerializer JSON { get; } = new JavaScriptSerializer();
        public static string ProgramFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Adit");
            }
        }

        public static bool IsJSONData(byte[] bytes)
        {
            return bytes[0] == 123 && bytes[1] == 34 && bytes[2] == 84 &&
                    bytes[3] == 121 && bytes[4] == 112 && bytes[5] == 101 &&
                    bytes[6] == 34 && bytes[7] == 58 && bytes[8] == 34;
        }

        public static List<string> SplitJSON(string inputString)
        {
            var messages = new List<string>(); ;
            while (inputString.IndexOf("}{") > -1)
            {
                var message = inputString.Substring(0, inputString.IndexOf("}{") + 1);
                messages.Add(message);
                inputString = inputString.Substring(inputString.IndexOf("}{") + 1);
            }
            messages.Add(inputString);
            return messages;
        }
        public static void WriteToLog(Exception ex)
        {
            var exception = ex;
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Adit_Logs.txt");
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                while (fi.Length > 1000000)
                {
                    var content = File.ReadAllLines(path);
                    File.WriteAllLines(path, content.Skip(10));
                    fi = new FileInfo(path);
                }
            }
            while (exception != null)
            {
                var jsonError = new
                {
                    Type = "Error",
                    Timestamp = DateTime.Now.ToString(),
                    exception?.Message,
                    exception?.Source,
                    exception?.StackTrace,
                };
                File.AppendAllText(path, JSON.Serialize(jsonError) + Environment.NewLine);
                exception = exception.InnerException;
            }
        }
        public static void WriteToLog(string Message)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Adit_Logs.txt");
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                while (fi.Length > 1000000)
                {
                    var content = File.ReadAllLines(path);
                    File.WriteAllLines(path, content.Skip(10));
                    fi = new FileInfo(path);
                }
            }
            var jsoninfo = new
            {
                Type = "Info",
                Timestamp = DateTime.Now.ToString(),
                Message = Message
            };
            File.AppendAllText(path, JSON.Serialize(jsoninfo) + Environment.NewLine);
        }
        public static void CleanupFiles()
        {
            var di = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "AditFiles"));
            foreach (var file in di.GetFiles())
            {
                if (DateTime.Now - file.LastWriteTime > TimeSpan.FromDays(1))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex);
                    }
                }
            }
        }

        public static string GetMACAddress()
        {
            return NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .Select(nic => nic.GetPhysicalAddress().ToString())
                        .FirstOrDefault();
        }
    }
}
