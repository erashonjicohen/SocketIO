using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SocketIO.Net.Abstractions
{
    public interface IConnection : IAsyncDisposable
    {
        EndPoint RemoteEndPoint { get; }

        ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default);

        ValueTask CloseAsync();
    }


}
