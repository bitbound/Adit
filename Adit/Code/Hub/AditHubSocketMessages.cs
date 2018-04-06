using Adit.Code.Shared;
using Adit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Hub
{
    public class AditHubSocketMessages : SocketMessageHandler
    {
        Socket socketOut;
        public AditHubSocketMessages(Socket socketOut)
            : base(socketOut)
        {
            this.socketOut = socketOut;
        }

        private void ReceiveEncryptionStatus(dynamic jsonData)
        {
            try
            {
                if (jsonData["Status"] == "On")
                {
                    Encryptor = new Encryption();
                    Encryptor.Key = Encryption.GetStoredKey();
                    if (Encryptor.Key == null)
                    {
                        AditHub.Current.Disconnect();
                        Pages.Hub.Current.RefreshUICall();
                        return;
                    }
                }
                else if (jsonData["Status"] == "Failed")
                {
                    throw new Exception("Server failed to start an encrypted connection.");
                }
                SendConnectionType(ConnectionTypes.ComputerHub);
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                System.Windows.MessageBox.Show("There was a problem starting an encrypted connection.  If the issue persists, please contact support.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }
        private void ReceiveRequestForHubCredentials(dynamic jsonData)
        {
            SendHubDataRequest();
        }
        public void SendHubDataRequest()
        {
            SendJSON(new
            {
                Type = "HubDataRequest",
                HubKey = Config.Current.HubKey
            });
        }
        private void ReceiveHubDataRequest(dynamic jsonData)
        {
            MainWindow.Current.Dispatcher.Invoke(() => {

                if (jsonData["Status"] != "ok")
                {
                    AditHub.Current.Disconnect();
                    MessageBox.Show(jsonData["Status"], "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    AditHub.Current.ComputerList.Clear();
                    var computerList = jsonData["ComputerList"] as object[];
                    foreach (Dictionary<string, object> clientObj in computerList)
                    {
                        var newClient = new HubComputer()
                        {
                            ID = (string)clientObj["ID"],
                            ComputerName = (string)clientObj["ComputerName"],
                            CurrentUser = (string)clientObj["CurrentUser"],
                            MACAddress = (string)clientObj["MACAddress"],
                            ConnectionType = (ConnectionTypes)clientObj["ConnectionType"],
                            LastReboot = (DateTime?)clientObj["LastReboot"],
                            SessionID = (string)clientObj["SessionID"],
                            Alias = (string)clientObj["Alias"],
                            LastOnline = (DateTime?)clientObj["LastOnline"],
                            IsOnline = (bool)clientObj["IsOnline"]
                        };
                        AditHub.Current.ComputerList.Add(newClient);
                    }
                    Pages.Hub.Current.RefreshUICall();
                }
            });
            
        }

        public void SendHubDisconnectClientRequest(System.Collections.IList clients)
        {
            SendJSON(new
            {
                Type = "HubDisconnectClientRequest",
                HubKey = Config.Current.HubKey,
                Clients = clients.Cast<HubComputer>().Select(x => x.ID)
            });
        }

        public void SendHubDeleteClientRequest(System.Collections.IList clients)
        {
            SendJSON(new
            {
                Type = "HubDeleteClientRequest",
                HubKey = Config.Current.HubKey,
                Clients = clients.Cast<HubComputer>().Select(x=>x.ID)
            });
        }
    }
}
