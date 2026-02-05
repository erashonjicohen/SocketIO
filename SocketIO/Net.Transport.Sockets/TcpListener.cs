using SocketIO.Net.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketIO.Net.Transport.Sockets
{
    public sealed class TcpListener : IListener
    {
        private readonly IPEndPoint _endpoint;
        private Socket? _listener;

        public TcpListener(IPEndPoint endpoint)
        {
            _endpoint = endpoint;
        }

        public Task StartAsync(CancellationToken ct = default)
        {
            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(_endpoint);
            _listener.Listen(100);
            return Task.CompletedTask;
        }

        public async Task<IConnection> AcceptAsync(CancellationToken ct = default)
        {
            if (_listener == null)
                throw new InvalidOperationException("Listener not started");

            var socket = await _listener.AcceptAsync(ct);
            return new TcpConnection(socket);
        }

        public ValueTask DisposeAsync()
        {
            _listener?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
