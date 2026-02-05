using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Diagnostics
{
    [Flags]
    public enum DumpDirection
    {
        None = 0,
        Rx = 1,
        Tx = 2,
        Both = Rx | Tx
    }
}
