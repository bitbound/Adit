using Adit.Models;
using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Adit.Code.Hub;

namespace Adit.Code.Server
{
    public class ServerSocketMessages : SocketMessageHandler
    {
        public ClientConnection ConnectionToClient { get; set; }
        public ClientSession Session { get; set; }
        public ServerSocketMessages(ClientConnection connection)
            : base(connection.Socket)
        {
            this.ConnectionToClient = connection;
        }

        private void ReceiveConnectionType(dynamic jsonData)
        {
            switch (jsonData["ConnectionType"])
            {
                case "Client":
                    ConnectionToClient.ConnectionType = ConnectionTypes.Client;
                    Session = new ClientSession();
                    Session.ConnectedClients.Add(ConnectionToClient);
                    AditServer.SessionList.Add(Session);
                    SendSessionID();
                    break;
                case "ElevatedClient":
                    ConnectionToClient.ConnectionType = ConnectionTypes.ElevatedClient;
                    Session = AditServer.SessionList.Find(x => x.SessionID == jsonData["SessionID"]);
                    if (Session == null)
                    {
                        Session = new ClientSession();
                        Session.SessionID = jsonData["SessionID"];
                    }
                    Session.ConnectedClients.Add(ConnectionToClient);
                    AditServer.SessionList.Add(Session);
                    break;
                case "Viewer":
                    ConnectionToClient.ConnectionType = ConnectionTypes.Viewer;
                    SendReadyForViewer();
                    break;
                case "Service":
                    ConnectionToClient.ConnectionType = ConnectionTypes.Service;
                    Session = new ClientSession();
                    Session.SessionID = Guid.NewGuid().ToString();
                    Session.ConnectedClients.Add(ConnectionToClient);
                    AditServer.SessionList.Add(Session);
                    break;
                case "ComputerHub":
                    ConnectionToClient.ConnectionType = ConnectionTypes.ComputerHub;
                    Session = new ClientSession();
                    Session.SessionID = Guid.NewGuid().ToString();
                    Session.ConnectedClients.Add(ConnectionToClient);
                    AditServer.SessionList.Add(Session);
                    SendRequestForHubCredentials();
                    break;
                default:
                    break;
            }
        }

        private void SendRequestForHubCredentials()
        {
            SendJSON(new
            {
                Type = "RequestForHubCredentials"
            });
        }

        private void SendSessionID()
        {
            SendJSON( new { Type = "SessionID", SessionID = Session.SessionID });
        }
        private void SendReadyForViewer()
        {
            SendJSON(new { Type = "ReadyForViewer" });
        }
        private void ReceiveViewerConnectRequest(dynamic jsonData)
        {
            var session = AditServer.SessionList.Find(x => x.SessionID.Replace(" ", "") == jsonData["SessionID"].Replace(" ", ""));
            if (session == null)
            {
                jsonData["Status"] = "notfound";
                SendJSON(jsonData);
                return;
            }
            if (session.ConnectedClients[0]?.ConnectionType == ConnectionTypes.Service)
            {
                SendRequestForElevatedClient(session.ConnectedClients[0]);
                return;
            }
            session.ConnectedClients.Add(ConnectionToClient);
            SendParticipantList(session);
            Session = session;
            jsonData["Status"] = "ok";
            SendJSON(jsonData);
        }

        public void SendEncryptionStatus()
        {
            try
            {
                if (Config.Current.IsEncryptionEnabled)
                {
                    Encryptor = new Encryption();
                    Encryptor.Key = Encryption.GetServerKey();
                    if (Encryptor.Key == null)
                    {
                        return;
                    }
                    SendJSONUnencrypted(new
                    {
                        Type = "EncryptionStatus",
                        Status = "On"
                    });
                }
                else
                {
                    SendJSON(new
                    {
                        Type = "EncryptionStatus",
                        Status = "Off"
                    });
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                SendJSON(new
                {
                    Type = "EncryptionStatus",
                    Status = "Failed"
                });
            }
        }

        private void SendRequestForElevatedClient(ClientConnection clientConnection)
        {
            var request = new
            {
                Type = "RequestForElevatedClient",
                RequesterID = this.ConnectionToClient.ID
            };
            clientConnection.SendJSON(request);
        }
        private async void ReceiveRequestForElevatedClient(dynamic jsonData)
        {
            if (jsonData["Status"] == "ok")
            {
                var startWait = DateTime.Now;
                while (AditServer.ClientList.Any(x => x.SessionID == jsonData["ClientSessionID"]) == false)
                {
                    await Task.Delay(500);
                    if (DateTime.Now - startWait > TimeSpan.FromSeconds(5))
                    {
                        jsonData["Status"] = "failed";
                        break;
                    }
                }
            }
            var requester = AditServer.ClientList.Find(x => x.ID == jsonData["RequesterID"]);
            requester.SendJSON(jsonData);
        }
        private void ReceiveDesktopSwitch(dynamic jsonData)
        {
            var session  = ConnectionToClient.SocketMessageHandler.Session;
            var startWait = DateTime.Now;
            jsonData["Status"] = "ok";

            while (AditServer.ClientList.Any(x => x.ConnectionType == ConnectionTypes.ElevatedClient && x.ID != ConnectionToClient.ID) == false)
            {
                System.Threading.Thread.Sleep(500);
                if (DateTime.Now - startWait > TimeSpan.FromSeconds(5))
                {
                    jsonData["Status"] = "failed";
                    break;
                }
            }
            AditServer.ClientList.RemoveAll(x => x.ID == ConnectionToClient.ID);
            ConnectionToClient.Socket.Close();
            SendParticipantList(session);
            foreach (var client in session.ConnectedClients.ToList())
            {
                client.SendJSON(jsonData);
            }
        }
        public void SendParticipantList(ClientSession session)
        {
            foreach (var connection in session.ConnectedClients)
            {
                connection.SendJSON(new
                {
                    Type = "ParticipantList",
                    ParticipantList = session.ConnectedClients.Select(x=>x.ID)
                });
            }
        }
        private void ReceiveCaptureRequest(dynamic jsonData)
        {
            jsonData["RequesterID"] = ConnectionToClient.ID;
            var clients = Session.ConnectedClients.FindAll(x => x.ConnectionType == ConnectionTypes.Client || x.ConnectionType == ConnectionTypes.ElevatedClient);
            foreach (var client in clients)
            {
                client.SendJSON(jsonData);
            }
        }
        private void ReceiveFullscreenRequest(dynamic jsonData)
        {
            if (Session == null)
            {
                return;
            }
            jsonData["RequesterID"] = ConnectionToClient.ID;
            var clients = Session.ConnectedClients.FindAll(x => x.ConnectionType == ConnectionTypes.Client || x.ConnectionType == ConnectionTypes.ElevatedClient);
            foreach (var client in clients)
            {
                client.SendJSON(jsonData);
            }
        }
        private void ReceiveStopCapture(dynamic jsonData)
        {
            jsonData["RequesterID"] = ConnectionToClient.ID;
            var clients = Session.ConnectedClients.FindAll(x => x.ConnectionType == ConnectionTypes.Client || x.ConnectionType == ConnectionTypes.ElevatedClient);
            foreach (var client in clients)
            {
                client.SendJSON(jsonData);
            }
        }
        public void SendCaptureRequest()
        {
            SendJSON(new
            {
                Type = "CaptureRequest",
                Fullscreen = false
            });
        }
        private void ReceiveSAS(dynamic jsonData)
        {
            var service = AditServer.ClientList.Find(x => x.ConnectionType == ConnectionTypes.Service && x.MACAddress == jsonData["MAC"]);
            service.SendJSON(jsonData);
        }

        private void ReceiveHeartbeat(dynamic jsonData)
        {
            ConnectionToClient.ComputerName = jsonData["ComputerName"]?.Trim();
            ConnectionToClient.LastReboot = jsonData["LastReboot"];
            ConnectionToClient.CurrentUser = jsonData["CurrentUser"];
            ConnectionToClient.MACAddress = jsonData["MACAddress"];
            Hub.AditHub.Current.AddOrUpdateComputer(ConnectionToClient);
        }
        private void ReceiveHubDataRequest(dynamic jsonData)
        {
            if (Authentication.Current.Keys.Count == 0)
            {
                jsonData["Status"] = "Server has no authentication keys.  Create an authentication key on the server first.";
                SendJSON(jsonData);
            }
            else if (!Authentication.Current.Keys.Any(x=>x.Key == jsonData["HubKey"]?.Trim()?.ToLower()))
            {
                jsonData["Status"] = "Authentication key wasn't found.";
                SendJSON(jsonData);
            }
            else
            {
                SendHubDataRequest(jsonData["HubKey"]);
            }
        }
        public void SendHubDataRequest(string hubKey)
        {
            var key = Authentication.Current.Keys.FirstOrDefault(x => x.Key == hubKey?.Trim()?.ToLower());
            key.LastUsed = DateTime.Now;
            Authentication.Current.Save();
            MainWindow.Current.Dispatcher.Invoke(() => {
                Hub.AditHub.Current.Load();
            });
            SendJSON(new
            {
                Type = "HubDataRequest",
                Status = "ok",
                ComputerList = Hub.AditHub.Current.ComputerList.ToList()
            });
        }

        private void ReceiveHubDisconnectClientRequest(dynamic jsonData)
        {
            if (!Authentication.Current.Keys.Any(x => x.Key == jsonData["HubKey"]?.Trim()?.ToLower()))
            {
                ConnectionToClient.Close();
                return;
            }
            foreach (var clientID in jsonData["Clients"])
            {
                var client = AditServer.ClientList.Find(x => x.ID == clientID);
                if (client != null)
                {
                    AditServer.ClientList.Remove(client);
                    client.Socket.Close();
                }
            }
            SendHubDataRequest(jsonData["HubKey"]);
        }

        private void ReceiveHubDeleteClientRequest(dynamic jsonData)
        {
            if (!Authentication.Current.Keys.Any(x => x.Key == jsonData["HubKey"]?.Trim()?.ToLower()))
            {
                ConnectionToClient.Close();
                return;
            }
            MainWindow.Current.Dispatcher.Invoke(() => {
                foreach (var client in jsonData["Clients"])
                {
                    var computer = AditHub.Current.ComputerList.FirstOrDefault(x => x.ID == client);
                    if (computer != null)
                    {
                        AditHub.Current.ComputerList.Remove(computer);
                    }
                }
                SendHubDataRequest(jsonData["HubKey"]);
            });
          
        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {
            if (ConnectionToClient.ConnectionType == ConnectionTypes.Client || ConnectionToClient.ConnectionType == ConnectionTypes.ElevatedClient)
            {
                foreach (var viewer in Session.ConnectedClients.Where(x=>x.ConnectionType == ConnectionTypes.Viewer))
                {
                    viewer.SendBytes(bytesReceived, viewer.ID);
                }
           
            }
            else if (ConnectionToClient.ConnectionType == ConnectionTypes.Viewer)
            {
                var session = AditServer.SessionList.Find(x => x.SessionID == ConnectionToClient.SessionID);
                var client = Session.ConnectedClients.Find(x => x.ConnectionType == ConnectionTypes.Client || x.ConnectionType == ConnectionTypes.ElevatedClient);
                client.SendBytes(bytesReceived, client.ID);
            }
        }
    }
}
