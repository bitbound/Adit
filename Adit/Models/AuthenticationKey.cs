using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Models
{
    public class AuthenticationKey
    {
        public string Key { get; set; } = Guid.NewGuid().ToString();
        public string IssuedTo { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime? LastUsed { get; set; }
    }
}
