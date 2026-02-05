using SocketIO.Net.Abstractions;
using SocketIO.Net.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SocketIO.Net.Diagnostics
{
    public sealed class WireTapConnection : IConnection
    {
        private readonly IConnection _inner;
        private readonly DumpSink _sink;
        private readonly DumpOptions _opt;

        public WireTapConnection(IConnection inner, DumpSink sink, DumpOptions options)
        {
            _inner = inner;
            _sink = sink;
            _opt = options;
        }

        public EndPoint RemoteEndPoint => _inner.RemoteEndPoint;

        public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            string text = BuildDumpText("TX", data.Span);
            await WriteDumpAsync(text, ct);

            await _inner.SendAsync(data, ct);
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            int read = await _inner.ReceiveAsync(buffer, ct);

            if (read > 0)
            {
                string text = BuildDumpText("RX", buffer.Span.Slice(0, read));
                await WriteDumpAsync(text, ct);
            }

            return read;
        }

        public ValueTask CloseAsync() => _inner.CloseAsync();
        public ValueTask DisposeAsync() => _inner.DisposeAsync();

        private ValueTask WriteDumpAsync(string text, CancellationToken ct)
        {
            return _sink.WriteAsync(text, ct);
        }


        private string BuildDumpText(string dir, ReadOnlySpan<byte> data)
        {
            if (!_opt.Enabled) return string.Empty;

            int len = Math.Min(data.Length, _opt.MaxBytesPerMessage);
            var slice = data.Slice(0, len);

            var parts = new List<string>(4);

            if (_opt.IncludeTimestamp)
                parts.Add(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            if (_opt.IncludeDirection)
                parts.Add(dir);

            parts.Add(RemoteEndPoint.ToString() ?? "remote?");
            parts.Add($"bytes={data.Length}");

            var header = string.Join(" | ", parts);
            var dump = HexDump.Format(slice, _opt.BytesPerLine);

            return header + "\n" + dump;
        }


    }
}
