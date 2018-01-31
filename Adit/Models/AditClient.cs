using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class AditClient
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Socket Socket { get; set; }
    }
}
