using SocketIO.Net.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Runtime
{
    public sealed class ConnectionManager
    {
        private readonly HashSet<IConnection> _connections = new();

        public void Add(IConnection conn) => _connections.Add(conn);

        public async Task CloseAllAsync()
        {
            foreach (var c in _connections)
                await c.CloseAsync();

            _connections.Clear();
        }
    }
}
