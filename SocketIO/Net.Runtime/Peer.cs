using SocketIO.Net.Abstractions;
using SocketIO.Net.Diagnostics;
using System.Net.Sockets;

namespace SocketIO.Net.Runtime
{
    public sealed class Peer
    {
        private readonly IConnection _connection;
        private readonly IFrameCodec _codec;
        public IConnection Connection => _connection;

        public Peer(IConnection connection, IFrameCodec codec)
        {
            _connection = connection;
            _codec = codec;
        }
        public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        {
            var frame = _codec.Encode(payload.Span);
            await _connection.SendAsync(frame, ct);
        }

        /// <summary>
        /// Recibe bytes del socket, decodifica frames con IFrameCodec, y entrega cada frame a onFrame.
        /// Si se provee dumper, hace dump por frame (no por chunk).
        /// Maneja desconexión graceful (read==0) y reset (SocketException 10054).
        /// </summary>
        public async Task ReceiveLoopAsync(
            Func<ReadOnlyMemory<byte>, Task> onFrame,
            FrameAwareDumper? dumper = null,
            CancellationToken ct = default)
        {
            var buffer = new byte[8192];
            int buffered = 0;
            int frameIndex = 0;

            string remote = _connection.RemoteEndPoint?.ToString() ?? "remote?";

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    int read;

                    try
                    {
                        read = await _connection.ReceiveAsync(buffer.AsMemory(buffered), ct);
                    }
                    catch (SocketException ex) when (
                        ex.SocketErrorCode == SocketError.ConnectionReset ||
                        ex.SocketErrorCode == SocketError.ConnectionAborted ||
                        ex.SocketErrorCode == SocketError.Shutdown ||
                        ex.SocketErrorCode == SocketError.OperationAborted)
                    {
                        // remoto cerró/abortó (ej: 10054)
                        break;
                    }

                    if (read == 0) // FIN graceful
                        break;

                    buffered += read;

                    ReadOnlySpan<byte> span = buffer.AsSpan(0, buffered);

                    // Parse frames SYNC (Span OK)
                    var frames = new List<ReadOnlyMemory<byte>>();

                    while (_codec.TryDecode(ref span, out var frame))
                        frames.Add(frame);

                    // compactar leftovers al inicio
                    span.CopyTo(buffer);
                    buffered = span.Length;

                    // Dispatch async (sin Span vivo)
                    foreach (var f in frames)
                    {
                        frameIndex++;

                        if (dumper is not null)
                            await dumper.DumpFrameAsync("RX", remote, frameIndex, f.Span);

                        await onFrame(f);
                    }
                }
            }
            finally
            {
                // opcional: cerrar siempre
                await _connection.CloseAsync();
            }
        }
        
      
    }
}
