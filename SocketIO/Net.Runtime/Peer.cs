using SocketIO.Net.Abstractions;
using SocketIO.Net.Diagnostics;
using System.Net.Sockets;

namespace SocketIO.Net.Runtime
{
    public sealed class Peer : IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IFrameCodec _codec;

        private int _disposed;

        public Peer(IConnection connection, IFrameCodec codec)
        {
            _connection = connection;
            _codec = codec;
        }

        public ValueTask CloseAsync() => _connection.CloseAsync();

        public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            var frame = _codec.Encode(payload.Span);
            await _connection.SendAsync(frame, ct);
        }

        public async Task ReceiveLoopAsync(
            Func<ReadOnlyMemory<byte>, Task> onFrame,
            FrameAwareDumper? dumper = null,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();

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
                        break;
                    }

                    if (read == 0) break;

                    buffered += read;

                    ReadOnlySpan<byte> span = buffer.AsSpan(0, buffered);

                    // Parse SYNC (Span OK) -> copiar frames a heap
                    var frames = new List<ReadOnlyMemory<byte>>();
                    while (_codec.TryDecode(ref span, out var frame))
                        frames.Add(frame);

                    // compact leftovers
                    span.CopyTo(buffer);
                    buffered = span.Length;

                    // Dispatch async
                    foreach (var f in frames)
                    {
                        frameIndex++;

                        if (dumper is not null)
                        {
                            // IMPORTANTE: que tu dumper NO reciba ReadOnlySpan en async.
                            await dumper.DumpFrameAsync("RX", remote, frameIndex, f, ct); // <- ReadOnlyMemory
                        }

                        await onFrame(f);
                    }
                }
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(Peer));
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            await _connection.DisposeAsync();
        }
    }
}

