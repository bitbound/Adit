using Adit.Code.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class Participant
    {
        public string ID { get; set; }
        public Capturer CaptureInstance { get; set; }
    }
}
