using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adit_Service
{
    public class SocketArgs : SocketAsyncEventArgs
    {
        public bool IsInUse { get; set; }
        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            base.OnCompleted(e);
            this.IsInUse = false;
        }
    }
}
