using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Adit.Code.Shared
{
    class Utilities
    {
        public static JavaScriptSerializer JSON { get; } = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
        public static string ProgramFolder
        {
            get
            {
                if (WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Adit");
                }
                else
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Adit");
                }
            }
        }
        public static string FileTransferFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "AditFiles");
            }
        }

        public static string CreateSessionID()
        {
            var random = new Random();
            return random.Next(0, 999).ToString().PadLeft(3, '0') + " " + random.Next(0, 999).ToString().PadLeft(3, '0');
        }
        public async static void ShowToolTip(FrameworkElement placementTarget, string message)
        {
            var tt = new System.Windows.Controls.ToolTip();
            tt.PlacementTarget = placementTarget;
            tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            tt.HorizontalOffset = Math.Round(placementTarget.ActualWidth * .25, 0) * -1;
            tt.VerticalOffset = Math.Round(placementTarget.ActualHeight * .5, 0);
            tt.Content = message;
            tt.Foreground = new SolidColorBrush(Colors.SteelBlue);
            tt.IsOpen = true;
            await Task.Delay(message.Length * 50);
            tt.BeginAnimation(FrameworkElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromSeconds(1)));
            await Task.Delay(1000);
            tt.IsOpen = false;
            tt = null;
        }
        

        public static bool IsJSONData(byte[] bytes)
        {
            return bytes[0] == 123 && bytes[1] == 34 && bytes[2] == 84 &&
                    bytes[3] == 121 && bytes[4] == 112 && bytes[5] == 101 &&
                    bytes[6] == 34 && bytes[7] == 58 && bytes[8] == 34;
        }

        internal static void DisplayErrorMessage()
        {
            if (Config.Current.StartupMode != Config.StartupModes.Notifier)
            {
                System.Windows.MessageBox.Show("There was an error during the last action.  If the issue persists, please contact support.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        public static string GetMACAddress()
        {
            return NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault();
        }
        public static void WriteToLog(Exception ex)
        {
            var exception = ex;
            var path = Path.GetTempPath() + "Adit_Logs.txt";
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
            var path = Path.GetTempPath() + "Adit_Logs.txt";
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
    }
}
