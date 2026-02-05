using System.Net;
using System.Net.Sockets;


namespace SocketIO.Net.Diagnostics
{
    public sealed class UdpSnifferRaw
    {
        private readonly DumpSink _sink;
        private readonly DumpOptions _opt;

        public UdpSnifferRaw(DumpSink sink, DumpOptions options)
        {
            _sink = sink;
            _opt = options;
        }

        public async Task RunAsync(int port, CancellationToken ct = default)
        {
            Console.WriteLine($"🕵️ UDP Sniffer RAW escuchando en {port}");

            using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Bind(new IPEndPoint(IPAddress.Any, port));

            var buffer = new byte[65535];


            while (!ct.IsCancellationRequested)
            {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

                // Nota: en algunas versiones de .NET el overload con CancellationToken no existe.
                // Este overload suele existir en .NET 6+ como ReceiveFromAsync(Memory<byte>, SocketFlags, EndPoint).
                var res = await sock.ReceiveFromAsync(buffer, SocketFlags.None, remote);

                var remoteStr = res.RemoteEndPoint!.ToString() ?? "remote?";

                if (_opt.Filter != null && !_opt.Filter.Match("RX", remoteStr, res.ReceivedBytes))
                    continue;

                var data = buffer.AsSpan(0, res.ReceivedBytes);

                // 1) construir texto sync (Span OK)
                var text = BuildDumpText("RX", res.RemoteEndPoint!, data);

                // 2) escribir async (sin Span)
                await _sink.WriteAsync(text, ct);
            }
        }

        private string BuildDumpText(string dir, EndPoint remote, ReadOnlySpan<byte> data)
        {
            
            if (!_opt.Enabled) return string.Empty;

            int len = Math.Min(data.Length, _opt.MaxBytesPerMessage);
            var slice = data.Slice(0, len);

            var parts = new List<string>(5);

            if (_opt.IncludeTimestamp)
                parts.Add(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            if (_opt.IncludeDirection)
                parts.Add(dir);

            parts.Add(remote.ToString() ?? "remote?");
            parts.Add($"bytes={data.Length}");

            var header = string.Join(" | ", parts);
            var dump = HexDump.Format(slice, _opt.BytesPerLine);

            return header + "\n" + dump;
        }
    }
}
