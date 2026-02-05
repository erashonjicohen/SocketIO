using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Abstractions
{
    public interface IListener : IAsyncDisposable
    {
        Task StartAsync(CancellationToken ct);
        Task<IConnection> AcceptAsync(CancellationToken ct);
    }

}
