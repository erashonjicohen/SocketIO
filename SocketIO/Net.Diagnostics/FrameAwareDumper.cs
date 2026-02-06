using SocketIO.Net.Diagnostics;

namespace SocketIO.Net.Diagnostics
{
    public sealed class FrameAwareDumper
    {
        private readonly DumpSink _sink;
        private readonly DumpOptions _opt;

        public FrameAwareDumper(DumpSink sink, DumpOptions opt)
        {
            _sink = sink;
            _opt = opt;
        }

        public ValueTask DumpFrameAsync(string dir, string remote, int frameIndex, ReadOnlySpan<byte> frame)
        {
            if (!_opt.Enabled) return ValueTask.CompletedTask;

            if (_opt.Filter != null && !_opt.Filter.Match(dir, remote, frame.Length))
                return ValueTask.CompletedTask;

            string text = BuildFrameDumpText(dir, remote, frameIndex, frame);
            return _sink.WriteAsync(text);
        }

        public async Task DumpFrameAsync(
            string dir,
            string remote,
            int frameIndex,
            ReadOnlyMemory<byte> frame,
            CancellationToken ct = default)
        {
            // SYNC: usar Span solo adentro
            string text = BuildDumpText(dir, remote, frameIndex, frame.Span);

            // ASYNC: escribir string
            await _sink.WriteAsync(text, ct);
        }
        private string BuildDumpText(
            string dir,
            string remote,
            int frameIndex,
            ReadOnlySpan<byte> data)
        {
            int len = Math.Min(data.Length, _opt.MaxBytesPerMessage);
            var slice = data.Slice(0, len);

            var header =
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} | " +
                $"{dir} | {remote} | frame={frameIndex} | bytes={data.Length}";

            var dump = HexDump.Format(slice, _opt.BytesPerLine);

            return header + "\n" + dump + "\n";
        }


        private string BuildFrameDumpText(string dir, string remote, int frameIndex, ReadOnlySpan<byte> frame)
        {
            if (!_opt.Enabled) return string.Empty;

            int len = Math.Min(frame.Length, _opt.MaxBytesPerMessage);
            var slice = frame.Slice(0, len);

            var parts = new List<string>(6);

            if (_opt.IncludeTimestamp)
                parts.Add(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            if (_opt.IncludeDirection)
                parts.Add(dir);

            parts.Add(remote);
            parts.Add($"frame#{frameIndex}");
            parts.Add($"frameBytes={frame.Length}");

            var header = string.Join(" | ", parts);
            var dump = HexDump.Format(slice, _opt.BytesPerLine);

            return header + "\n" + dump;
        }
    }
}
