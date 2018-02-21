using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Code.Server
{
    public class ComputerHub
    {
        public static ComputerHub Current { get; set; } = new ComputerHub();

        public List<HubComputer> ComputerList { get; set; } = new List<HubComputer>();

        public void AddOrUpdateComputer(ClientConnection connection)
        {
            var onlineComputer = ComputerList.Find(x =>!string.IsNullOrWhiteSpace(x.ComputerName) && x.ComputerName == connection.ComputerName);
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
                ComputerList.Add(onlineComputer);
            }
            onlineComputer.ID = connection.ID;
            onlineComputer.SessionID = connection.SessionID;
            onlineComputer.ConnectionType = connection.ConnectionType;
            onlineComputer.IsOnline = true;
            onlineComputer.CurrentUser = connection.CurrentUser;
            onlineComputer.ComputerName = connection.ComputerName;
            onlineComputer.LastOnline = DateTime.Now;
            onlineComputer.LastReboot = connection.LastReboot;
            onlineComputer.Alias = string.IsNullOrWhiteSpace(connection.Alias) ? onlineComputer.Alias : connection.Alias;
            Save();
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
        }

        public void Save()
        {
            var di = Directory.CreateDirectory(Utilities.ProgramFolder);
            var computerList = ComputerList.Where(x => !string.IsNullOrWhiteSpace(x.ComputerName)).ToList();
            for (var i = computerList.Count - 1; i >= 0; i--)
            {
                if (computerList.FindAll(x => x.ComputerName == computerList[i].ComputerName).Count > 1)
                {
                    computerList.RemoveAt(i);
                }
            }

            File.WriteAllText(Path.Combine(di.FullName, "Hub.json"), Utilities.JSON.Serialize(computerList));
        }

        public void Load()
        {
            ComputerList.Clear();
            var fi = new FileInfo(Path.Combine(Utilities.ProgramFolder, "Hub.json"));
            if (fi.Exists)
            {
                ComputerList.AddRange(Utilities.JSON.Deserialize<List<HubComputer>>(File.ReadAllText(fi.FullName)));
            }
            MergeAditServerClientList();
        }
    }
}
