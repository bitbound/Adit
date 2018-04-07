using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Code.Server
{
    public class Authentication
    {
        public static Authentication Current { get; } = new Authentication();

        public Authentication()
        {
            Load();
            Keys.CollectionChanged += Keys_CollectionChanged;
        }

        private void Keys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        public ObservableCollection<AuthenticationKey> Keys { get; set; } = new ObservableCollection<AuthenticationKey>();

        public void Save()
        {
            var di = Directory.CreateDirectory(Utilities.DataFolder);
            File.WriteAllText(Path.Combine(di.FullName, "AuthKeys.json"), Utilities.JSON.Serialize(Keys));
            File.Encrypt(Path.Combine(di.FullName, "AuthKeys.json"));
        }

        public void Load()
        {
            var fi = new FileInfo(Path.Combine(Utilities.DataFolder, "AuthKeys.json"));
            if (fi.Exists)
            {
                try
                {
                    foreach (var key in Utilities.JSON.Deserialize<ObservableCollection<AuthenticationKey>>(File.ReadAllText(fi.FullName)))
                    {
                        Keys.Add(key);
                    }

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Unable to read authentication key file.  You may not have access to it.  The file can only be read by the account that created it.", "Read Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    Utilities.WriteToLog(ex);
                }
            }
        }
    }
}
