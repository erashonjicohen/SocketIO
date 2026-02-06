using SocketIO.Net.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Protocol.Codec
{
    public sealed class KiSoftFrameCodec : IFrameCodec
    {
        private const byte LF = 0x0A;
        private const byte CR = 0x0D;

        public ReadOnlyMemory<byte> Encode(ReadOnlySpan<byte> payload)
        {
            // LEN = bytes(payload) + bytes(LEN5)   (según doc)
            int totalLen = payload.Length + 5;

            if (totalLen < 6 || totalLen > 99999)
                throw new ArgumentOutOfRangeException(nameof(payload), "Longitud fuera de 00006–99999.");

            Span<byte> lenAscii = stackalloc byte[5];
            // 5 chars, zero padded
            Encoding.ASCII.GetBytes(totalLen.ToString("D5"), lenAscii);

            var buf = new byte[1 + 5 + payload.Length + 1];
            buf[0] = LF;
            lenAscii.CopyTo(buf.AsSpan(1, 5));
            payload.CopyTo(buf.AsSpan(6));
            buf[^1] = CR;
            return buf;
        }

        public bool TryDecode(ref ReadOnlySpan<byte> buffer, out ReadOnlyMemory<byte> frame)
        {
            frame = default;

            // Necesitamos mínimo: LF + 5 + CR (pero payload min 1 => total >= 7)
            if (buffer.Length < 7) return false;

            // Buscar LF (resync básico)
            int start = buffer.IndexOf(LF);
            if (start < 0)
            {
                buffer = ReadOnlySpan<byte>.Empty;
                return false;
            }

            buffer = buffer.Slice(start);

            if (buffer.Length < 7) return false;
            if (buffer[0] != LF) return false;

            // Parse LEN5
            var lenSpan = buffer.Slice(1, 5);
            if (!TryParseLen5(lenSpan, out int totalLen)) return false;

            // totalLen = payloadBytes + 5
            int payloadLen = totalLen - 5;
            if (payloadLen < 1 || totalLen > 99999) { buffer = buffer.Slice(1); return false; }

            int fullFrameLen = 1 + 5 + payloadLen + 1;
            if (buffer.Length < fullFrameLen) return false;

            if (buffer[fullFrameLen - 1] != CR)
            {
                // frame corrupta, mover 1 y re-sincronizar
                buffer = buffer.Slice(1);
                return false;
            }

            // ✅ sacar payload (sin LF/LEN/CR)
            var payload = buffer.Slice(6, payloadLen).ToArray();
            frame = payload;

            buffer = buffer.Slice(fullFrameLen);
            return true;
        }

        private static bool TryParseLen5(ReadOnlySpan<byte> len5, out int totalLen)
        {
            totalLen = 0;
            for (int i = 0; i < 5; i++)
            {
                byte c = len5[i];
                if (c < (byte)'0' || c > (byte)'9') return false;
                totalLen = (totalLen * 10) + (c - (byte)'0');
            }
            return true;
        }
    }
}
