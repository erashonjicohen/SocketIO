using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Security
{
    public sealed class SodiumCryptoProvider : ICryptoProvider
    {
        public byte[] Encrypt(ReadOnlySpan<byte> data)
            => data.ToArray(); // placeholder

        public byte[] Decrypt(ReadOnlySpan<byte> data)
            => data.ToArray(); // placeholder
    }
}
