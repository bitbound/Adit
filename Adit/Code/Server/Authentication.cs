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
            var di = Directory.CreateDirectory(Utilities.ProgramFolder);
            File.WriteAllText(Path.Combine(di.FullName, "Auth.json"), Utilities.JSON.Serialize(Keys));
        }

        public void Load()
        {
            var fi = new FileInfo(Path.Combine(Utilities.ProgramFolder, "Auth.json"));
            if (fi.Exists)
            {
                foreach (var key in Utilities.JSON.Deserialize<ObservableCollection<AuthenticationKey>>(File.ReadAllText(fi.FullName)))
                {
                    Keys.Add(key);
                }
            }
        }
    }
}
