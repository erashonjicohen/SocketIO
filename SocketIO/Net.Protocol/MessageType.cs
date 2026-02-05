using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Protocol
{
    public enum MessageType : byte
    {
        Hello = 1,
        Ping = 2,
        Pong = 3,
        Data = 4,
        Error = 255
    }
}
