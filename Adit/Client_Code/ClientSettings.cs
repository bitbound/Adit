using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Client_Code
{
    public class ClientSettings
    {
        public static ClientSettings Current { get; set; } = new ClientSettings();

        public string Host { get; set; }
        public int Port { get; set; }
    }
}
