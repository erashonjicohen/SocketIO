using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Abstractions
{
    public interface IFrameCodec
    {
        ReadOnlyMemory<byte> Encode(ReadOnlySpan<byte> payload);
        bool TryDecode(ref ReadOnlySpan<byte> buffer, out ReadOnlyMemory<byte> frame);
    }
}
