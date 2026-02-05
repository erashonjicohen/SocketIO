using SocketIO.Net.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketIO.Net.Transport.Sockets
{
    public sealed class UdpConnection : IConnection
    {
        private readonly Socket _socket;
        private readonly EndPoint _remote;

        public UdpConnection(Socket socket, EndPoint remote)
        {
            _socket = socket;
            _remote = remote;
        }

        public EndPoint RemoteEndPoint => _remote;

        public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            await _socket.SendToAsync(data, SocketFlags.None, _remote, ct);
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
            => _socket.ReceiveAsync(buffer, SocketFlags.None, ct);

        public ValueTask CloseAsync()
        {
            _socket.Close();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _socket.Dispose();
            return ValueTask.CompletedTask;
        }
    }


}
