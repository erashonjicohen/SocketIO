using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Abstractions
{

    public interface IPacket
    {
        byte Type { get; }
        ReadOnlyMemory<byte> Payload { get; }
    }

}
