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
                    // Create ClientSession and add to list.
                    Session = new ClientSession();
                    Session.ConnectedClients.Add(ConnectionToClient);
                    AditServer.SessionList.Add(Session);
                    SendSessionID();
                    break;
                case "Viewer":
                    ConnectionToClient.ConnectionType = ConnectionTypes.Viewer;
                    SendReadyForViewer();
                    break;
                case "Service":
                    ConnectionToClient.ConnectionType = ConnectionTypes.Service;
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
                SendBytes(jsonData);
                return;
            }
            session.ConnectedClients.Add(ConnectionToClient);
            SendParticipantList(session);
            Session = session;
            jsonData["Status"] = "ok";
            SendJSON(jsonData);
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
            Session.ConnectedClients.Find(x => x.ConnectionType == ConnectionTypes.Client)?.SendJSON(jsonData);

        }

        private void ReceiveHeartbeat(dynamic jsonData)
        {
            ConnectionToClient.ComputerName = jsonData["ComputerName"]?.Trim();
            ConnectionToClient.LastReboot = jsonData["LastReboot"];
            ConnectionToClient.CurrentUser = jsonData["CurrentUser"];
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
                jsonData["Status"] = "ok";
                ComputerHub.Current.Load();
                ComputerHub.Current.Save();
                jsonData["ComputerList"] = ComputerHub.Current.ComputerList;
            }
            SendJSON(jsonData);
        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {
            var requesterID = Encoding.UTF8.GetString(bytesReceived.Take(36).ToArray());
            var requester = AditServer.ClientList.Find(x => x.ID == requesterID);
            requester?.SendBytes(bytesReceived.Skip(36).ToArray());
        }
    }
}
