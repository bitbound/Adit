using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    class ClientSession
    {
        public string SessionID { get; set; } = Guid.NewGuid().ToString();

        public List<ClientConnection> ConnectedClients { get; set; }
    }
}
