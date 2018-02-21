using Adit.Code.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class HubComputer
    {
        public bool IsOnline { get; set; }
        public string IsOnline2 {
            get
            {
                return IsOnline ? "Yes" : "No";
            }
        }
        public string ID { get; set; }
        public string SessionID { get; set; }
        public string ComputerName { get; set; }
        public string CurrentUser { get; set; }
        public string Alias { get; set; }
        public DateTime? LastReboot { get; set; }
        public DateTime? LastOnline { get; set; }
        public ConnectionTypes ConnectionType { get; set; }
    }
}
