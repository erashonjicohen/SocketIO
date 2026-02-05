using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Protocol
{
    public sealed class PacketWriter
    {
        private readonly MemoryStream _stream = new();

        public void Write(byte value) => _stream.WriteByte(value);

        public void Write(ReadOnlySpan<byte> data)
            => _stream.Write(data);

        public byte[] ToArray() => _stream.ToArray();
    }
}
