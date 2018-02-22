using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Adit.Code.Shared
{
    public class ClipboardManager
    {
        public static ClipboardManager Current { get; set; } = new ClipboardManager();

        private System.Timers.Timer ClipboardWatcher { get; set; }
        private IDataObject ClipboardData { get; set; } = new DataObject();


        public void BeginWatching(Models.SocketMessageHandler connectionToSendChanges)
        {
            if (ClipboardWatcher?.Enabled == true)
            {
                ClipboardWatcher.Stop();
            }
            ClipboardWatcher = new System.Timers.Timer(500);
            ClipboardWatcher.Elapsed += (sender, args) => 
            {
                MainWindow.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (!Clipboard.IsCurrent(ClipboardData))
                        {
                            ClipboardData = Clipboard.GetDataObject();
                            var jsonData = GetTransferData();
                            if (!string.IsNullOrWhiteSpace(jsonData.Format))
                            {
                                connectionToSendChanges.SendJSON(jsonData);
                            }
                        }
                    }
                    catch
                    {
                        if (!connectionToSendChanges.IsConnected)
                        {
                            ClipboardWatcher.Stop();
                        }
                    }
                });
                
            };
            ClipboardWatcher.Start();
        }

        public void StopWatching()
        {
            ClipboardWatcher.Stop();
        }

        private dynamic GetTransferData()
        {
            var formats = ClipboardData.GetFormats();
            var format = string.Empty;
            var dataString = String.Empty;
            if (formats.Contains("Text"))
            {
                format = "Text";
                dataString = ClipboardData.GetData("Text") as string;
                ClipboardData = new DataObject("Text", dataString, true);
            }
            else if (formats.Contains("Bitmap"))
            {
                format = "Bitmap";
                var encoder = new PngBitmapEncoder();
                var frame = BitmapFrame.Create(ClipboardData.GetData("Bitmap") as InteropBitmap);
                encoder.Frames.Add(frame);
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    dataString = Convert.ToBase64String(stream.ToArray());
                }
            }
            else if (formats.Contains("FileDrop"))
            {
                format = "FileDrop";
                var fileNames = new List<string>();
                var fileContents = new List<string>();
                var filePaths = ClipboardData.GetData("FileDrop") as string[];
                foreach (var filePath in filePaths)
                {
                    fileNames.Add(Path.GetFileName(filePath));
                    fileContents.Add(Convert.ToBase64String(File.ReadAllBytes(filePath)));
                }
                ClipboardData = new DataObject("FileDrop", filePaths.ToArray(), true);
                Clipboard.SetDataObject(ClipboardData);
                return new
                {
                    Type = "ClipboardTransfer",
                    Format = "FileDrop",
                    FileNames = fileNames,
                    FileContents = fileContents
                };
            }
            Clipboard.SetDataObject(ClipboardData);
            return new
            {
                Type = "ClipboardTransfer",
                Format = format,
                Data = dataString
            };
        }
        public void SetData(string format, string data)
        {
            MainWindow.Current.Dispatcher.Invoke(() =>
            {
                if (format == "Text")
                {
                    ClipboardData = new DataObject(format, data, true);
                }
                else if (format == "Bitmap")
                {
                    using (var ms = new MemoryStream(Convert.FromBase64String(data)))
                    {
                        var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        ClipboardData = new DataObject(format, decoder.Frames[0], true);
                    }
                }
                Clipboard.SetDataObject(ClipboardData);
            });
        }
        public void SetFiles(object[] fileNames, object[] fileContents)
        {
            MainWindow.Current.Dispatcher.Invoke(() =>
            {
                var dropList = new List<string>();
                for (var i = 0; i < fileNames.Length; i++)
                {
                    var fileName = fileNames[i];
                    var data = fileContents[i];
                    File.WriteAllBytes(Path.Combine(Utilities.FileTransferFolder, fileName.ToString()), Convert.FromBase64String(data.ToString()));
                    dropList.Add(Path.Combine(Utilities.FileTransferFolder, fileName.ToString()));
                }
                ClipboardData = new DataObject("FileDrop", dropList.ToArray(), true);
                Clipboard.SetDataObject(ClipboardData);
            });
        }
    }
}
