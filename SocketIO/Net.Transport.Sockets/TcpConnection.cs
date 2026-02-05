using SocketIO.Net.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace SocketIO.Net.Transport.Sockets
{
    public sealed class TcpConnection : IConnection
    {
        private readonly Socket _socket;

        public TcpConnection(Socket socket)
        {
            _socket = socket;
        }

        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint!;

        // SendAsync → descarta el int (bytes enviados)
        public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            await _socket.SendAsync(data, SocketFlags.None, ct);
        }

        // ReceiveAsync → devuelve el int
        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
            => _socket.ReceiveAsync(buffer, SocketFlags.None, ct);

        public ValueTask CloseAsync()
        {
            if (_socket.Connected)
                _socket.Shutdown(SocketShutdown.Both);

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
