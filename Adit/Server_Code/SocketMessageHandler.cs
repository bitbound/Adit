using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Server_Code
{
    public class SocketMessageHandler
    {
        public static void ProcessSocketMessage(byte[] buffer)
        {
            var trimmedBuffer = Utilities.TrimBytes(buffer);
            var stringMessage = Encoding.UTF8.GetString(trimmedBuffer);
        }
    }
}
