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
    public class ComputerHub
    {
        public static ComputerHub Current { get; set; } = new ComputerHub();

        public ObservableCollection<HubComputer> ComputerList { get; set; } = new ObservableCollection<HubComputer>();

        public ComputerHub()
        {
            Load();
            ComputerList.CollectionChanged += ComputerList_CollectionChanged;
        }

        private void ComputerList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Save();
        }

        public void AddOrUpdateComputer(ClientConnection connection)
        {
            var onlineComputer = ComputerList.FirstOrDefault(x =>!string.IsNullOrWhiteSpace(x.MACAddress) && x.MACAddress == connection.MACAddress);
            if (onlineComputer != null)
            {
                onlineComputer.IsOnline = true;
            }
            else
            {
                onlineComputer = new HubComputer()
                {
                    IsOnline = true
                };
                MainWindow.Current.Dispatcher.Invoke(() =>
                {
                    ComputerList.Add(onlineComputer);
                });
            }
            onlineComputer.ID = connection.ID;
            onlineComputer.SessionID = connection.SessionID;
            onlineComputer.ConnectionType = connection.ConnectionType;
            onlineComputer.IsOnline = true;
            onlineComputer.CurrentUser = connection.CurrentUser;
            onlineComputer.ComputerName = connection.ComputerName;
            onlineComputer.LastOnline = DateTime.Now;
            onlineComputer.LastReboot = connection.LastReboot;
            onlineComputer.MACAddress = connection.MACAddress;
        }


        public void MergeAditServerClientList()
        {
            foreach (var computer in ComputerList)
            {
                computer.IsOnline = false;
            }
            foreach (var connection in AditServer.ClientList)
            {
                AddOrUpdateComputer(connection);
            }
            for (var i = ComputerList.Count - 1; i >= 0; i--)
            {
                var computer = ComputerList[i];
                if (!computer.IsOnline && string.IsNullOrWhiteSpace(computer.MACAddress))
                {
                    ComputerList.Remove(computer);
                }
            }
        }

        public void Save()
        {
            var di = Directory.CreateDirectory(Utilities.DataFolder);
            var computerList = ComputerList.Where(x => !string.IsNullOrWhiteSpace(x.MACAddress)).ToList();
            for (var i = computerList.Count - 1; i >= 0; i--)
            {
                if (computerList.FindAll(x => x.MACAddress == computerList[i].MACAddress).Count > 1)
                {
                    computerList.RemoveAt(i);
                }
            }

            File.WriteAllText(Path.Combine(di.FullName, "Hub.json"), Utilities.JSON.Serialize(computerList));
        }

        public void Load()
        {
            var fi = new FileInfo(Path.Combine(Utilities.DataFolder, "Hub.json"));
            if (fi.Exists)
            {
                var savedList = Utilities.JSON.Deserialize<List<HubComputer>>(File.ReadAllText(fi.FullName)).Where(x => !string.IsNullOrWhiteSpace(x.MACAddress));
                foreach (var computer in savedList)
                {
                    if (!ComputerList.Any(x=>x.MACAddress == computer.MACAddress))
                    {
                        ComputerList.Add(computer);
                    }
                }
            }
            MergeAditServerClientList();
        }
    }
}
