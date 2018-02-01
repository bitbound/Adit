using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Adit.Shared_Code
{
    class Utilities
    {
        public static JavaScriptSerializer JSON { get; } = new JavaScriptSerializer();
        public async static void ShowToolTip(FrameworkElement placementTarget, string message, System.Windows.Media.Color fontColor)
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
        
        public static byte[] TrimBytes(byte[] bytes)
        {
            // Loop backwards through array until the first non-zero byte is found.
            var firstZero = 0;
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                if (bytes[i] != 0)
                {
                    firstZero = i + 1;
                    break;
                }
            }
            if (firstZero == 0)
            {
                throw new Exception("Byte array is empty.");
            }
            // Return non-empty bytes.
            return bytes.Take(firstZero).ToArray();
        }
        public static void WriteToLog(Exception ex, string appMode)
        {
            var exception = ex;
            var path = Path.GetTempPath() + $"Adit_{appMode}_Logs.txt";
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
        public static void WriteToLog(string Message, string appMode)
        {
            var path = Path.GetTempPath() + $"Adit_{appMode}_Logs.txt";
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
