using SocketIO.Net.Abstractions;
using SocketIO.Net.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace SocketIO.Net.Transport.Sockets
{
    public sealed class TcpConnection : IConnection
    {
        private readonly Socket _socket;
        private readonly Logger _logger;

        public TcpConnection(Socket socket, Logger? logger = null)
        {
            _socket = socket;
            _logger = logger ?? new Logger(new LoggerOptions { WriteToConsole = true });
        }

        // =========================
        // Helper: ConnectAsync
        // =========================
        public async Task<TcpConnection> ConnectAsync(
            EndPoint remoteEndPoint,
            CancellationToken ct = default)
        {
            var socket = new Socket(
                remoteEndPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            try
            {
                await socket.ConnectAsync(remoteEndPoint, ct);
                return new TcpConnection(socket);
            }
            catch(Exception ex)
            {
                if (_logger is null)
                    Console.WriteLine($"Error al conectar al endpoint remoto. {ex}");
                else
                    _logger.Error("Error al conectar al endpoint remoto. {0}", ex);

                socket.Dispose();
                throw;
            }
        }

        public async Task<(bool IsConnected, TcpConnection? Connection)> TryConnectAsync(
        EndPoint remoteEndPoint, CancellationToken ct = default
        )
        {
            TcpConnection? connection = null;
            bool isConnected = false;
            var socket = new Socket(
                remoteEndPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            try
            {
                await socket.ConnectAsync(remoteEndPoint, ct);
                connection = new TcpConnection(socket);
                isConnected = true;
                return (isConnected, connection);
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                isConnected = false;
                return (isConnected, connection);
            }
            catch (SocketException ex)
            {
                // 10061 = actively refused (no server escuchando)
                _logger.Error($"❌ TCP connect failed ({ex.SocketErrorCode}) => {remoteEndPoint}");
                socket.Dispose();
                isConnected = false;
                return (isConnected, connection);
            }
            catch (Exception ex)
            {
                _logger.Error($"❌ TCP connect failed => {remoteEndPoint}. {ex.Message}");
                socket.Dispose();
                isConnected = false;
                return (isConnected, connection);
            }
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
            try
            {
                if (_socket.Connected)
                    _socket.Shutdown(SocketShutdown.Both);
            }
            catch { /* ignore */ }

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
