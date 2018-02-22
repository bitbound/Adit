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
                    Session = new ClientSession();
                    Session.SessionID = jsonData["SessionID"];
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
                default:
                    break;
            }
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
        private async void ReceiveDesktopSwitch(dynamic jsonData)
        {
            var startWait = DateTime.Now;
            while (AditServer.ClientList.Any(x => x.SessionID == jsonData["SessionID"]) == false)
            {
                await Task.Delay(500);
                if (DateTime.Now - startWait > TimeSpan.FromSeconds(5))
                {
                    jsonData["Status"] = "failed";
                    break;
                }
            }
            jsonData["Status"] = "ok";
            var session = AditServer.SessionList.Find(x => x.SessionID == ConnectionToClient.SessionID);
            var newSession = AditServer.SessionList.Find(x => x.SessionID == jsonData["SessionID"]);

            while (session.ConnectedClients.Any(x=>x.ConnectionType == ConnectionTypes.Viewer))
            {
                var viewer = session.ConnectedClients.FirstOrDefault(x => x.ConnectionType == ConnectionTypes.Viewer);
                session.ConnectedClients.Remove(viewer);
                newSession.ConnectedClients.Add(viewer);
                viewer.SocketMessageHandler.Session = newSession;
                viewer.SendJSON(jsonData);
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
        private void ReceiveImageRequest(dynamic jsonData)
        {
            jsonData["RequesterID"] = ConnectionToClient.ID;
            Session.ConnectedClients.Find(x => x.ConnectionType == ConnectionTypes.Client || x.ConnectionType == ConnectionTypes.ElevatedClient)?.SendJSON(jsonData);
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
            ComputerHub.Current.AddOrUpdateComputer(ConnectionToClient);
        }
        private void ReceiveHubDataRequest(dynamic jsonData)
        {
            if (Authentication.Current.Keys.Count == 0)
            {
                jsonData["Status"] = "Server has no authentication keys.  Create an authentication key on the server first.";
            }
            else if (!Authentication.Current.Keys.Any(x=>x.Key == jsonData["Key"]?.Trim()?.ToLower()))
            {
                jsonData["Status"] = "Authentication key wasn't found.";
            }
            else
            {
                var key = Authentication.Current.Keys.FirstOrDefault(x => x.Key == jsonData["Key"]?.Trim()?.ToLower());
                key.LastUsed = DateTime.Now;
                Authentication.Current.Save();
                jsonData["Status"] = "ok";
                ComputerHub.Current.Load();
                jsonData["ComputerList"] = ComputerHub.Current.ComputerList;
            }
            SendJSON(jsonData);
        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {
            if (ConnectionToClient.ConnectionType == ConnectionTypes.Client || ConnectionToClient.ConnectionType == ConnectionTypes.ElevatedClient)
            {
                var requesterID = Encoding.UTF8.GetString(bytesReceived.Take(36).ToArray());
                var requester = AditServer.ClientList.Find(x => x.ID == requesterID);
                requester?.SendBytes(bytesReceived.Skip(36).ToArray());
            }
            else if (ConnectionToClient.ConnectionType == ConnectionTypes.Viewer)
            {
                var session = AditServer.SessionList.Find(x => x.SessionID == ConnectionToClient.SessionID);
                var client = Session.ConnectedClients.Find(x => x.ConnectionType == ConnectionTypes.Client || x.ConnectionType == ConnectionTypes.ElevatedClient);
                client.SendBytes(bytesReceived);
            }
        }
    }
}
