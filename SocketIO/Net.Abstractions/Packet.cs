using SocketIO.Net.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Abstractions
{
    public readonly record struct Packet(
        byte Version,
        MessageType Type,
        ushort Flags,
        uint Sequence,
        ReadOnlyMemory<byte> Payload
    ) : IPacket
    {
        byte IPacket.Type => (byte)Type;
    }
}
