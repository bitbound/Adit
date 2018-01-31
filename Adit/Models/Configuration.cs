using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class ClientConfiguration
    {
        public string TargetServerHost { get; set; }
        public int TargetServerPort { get; set; }
        public bool IsViewerAvailable { get; set; }
    }
}
