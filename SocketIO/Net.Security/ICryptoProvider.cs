using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Security
{

    public interface ICryptoProvider
    {
        byte[] Encrypt(ReadOnlySpan<byte> data);
        byte[] Decrypt(ReadOnlySpan<byte> data);
    }
}
