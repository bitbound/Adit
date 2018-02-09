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
        ClientConnection connectionToClient;
        ClientSession Session { get; set; }
        public ServerSocketMessages(ClientConnection connection)
            : base(connection.Socket)
        {
            this.connectionToClient = connection;
        }
        private void ReceiveConnectionType(dynamic jsonData)
        {
            switch (jsonData["ConnectionType"])
            {
                case "Client":
                    connectionToClient.ConnectionType = ConnectionTypes.Client;
                    // Create ClientSession and add to list.
                    Session = new ClientSession();
                    Session.ConnectedClients.Add(connectionToClient);
                    AditServer.SessionList.Add(Session);
                    SendSessionID();
                    break;
                case "Viewer":
                    connectionToClient.ConnectionType = ConnectionTypes.Viewer;
                    SendReadyForViewer();
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
            SendPartnerCount(session);
            session.ConnectedClients.Add(connectionToClient);
            Session = session;
            jsonData["Status"] = "ok";
            SendJSON(jsonData);
        }
        private void SendPartnerCount(ClientSession session)
        {
            foreach (var connection in session.ConnectedClients)
            {
                connection.SendJSON(new
                {
                    Type = "PartnerCount",
                    PartnerCount = session.ConnectedClients.Count
                });
            }
        }
        private void ReceiveImageRequest(dynamic jsonData)
        {
            jsonData["RequesterID"] = connectionToClient.ID;
            Session.ConnectedClients.Find(x => x.ConnectionType == ConnectionTypes.Client).SendJSON(jsonData);

        }
        private void ReceiveByteArray(byte[] bytesReceived)
        {
            var requesterID = Encoding.UTF8.GetString(bytesReceived.Take(36).ToArray());
            var requester = AditServer.ClientList.Find(x => x.ID == requesterID);
            requester.SendBytes(bytesReceived.Skip(36).ToArray());
        }
    }
}
