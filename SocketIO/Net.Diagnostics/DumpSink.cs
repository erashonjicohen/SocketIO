using System;
using System.Collections.Generic;
using System.Text;

namespace SocketIO.Net.Diagnostics
{
    public sealed class DumpSink : IAsyncDisposable
    {
        private readonly DumpOptions _opt;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private StreamWriter? _writer;

        public DumpSink(DumpOptions options)
        {
            _opt = options;

            if (!string.IsNullOrWhiteSpace(_opt.FilePath))
            {
                _writer = new StreamWriter(File.Open(_opt.FilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true,
                    NewLine = "\n"
                };
            }
        }

        public async ValueTask WriteAsync(string text, CancellationToken ct = default)
        {
            if (!_opt.Enabled) return;

            if (_opt.WriteToConsole)
                Console.WriteLine(text);

            if (_writer is null) return;

            await _gate.WaitAsync(ct);
            try
            {
                await _writer.WriteLineAsync(text);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_writer is not null)
                await _writer.DisposeAsync();
            _gate.Dispose();
        }
    }
}
