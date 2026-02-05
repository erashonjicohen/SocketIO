using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Protocol
{
    public ref struct PacketReader
    {
        private ReadOnlySpan<byte> _buffer;

        public PacketReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
        }

        public byte ReadByte()
        {
            var b = _buffer[0];
            _buffer = _buffer[1..];
            return b;
        }

        public ReadOnlySpan<byte> ReadRemaining()
        {
            var data = _buffer;
            _buffer = ReadOnlySpan<byte>.Empty;
            return data;
        }
    }

}
