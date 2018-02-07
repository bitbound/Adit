using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class ClientSession
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();

        public string SessionID { get; set; } = Utilities.CreateSessionID();

        public List<ClientConnection> ConnectedClients { get; set; } = new List<ClientConnection>();
    }
}
