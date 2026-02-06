
using System.Text;


namespace SocketIO.Net.Diagnostics
{
  

        public sealed class DumperFile : IAsyncDisposable
        {
            private readonly DumpOptions _options;
            private readonly SemaphoreSlim _gate = new(1, 1);
            private FileStream? _stream;
            private StreamWriter? _writer;
            private readonly Logger _logger;

            public DumperFile(DumpOptions options, Logger logger)
            {
                _options = options ?? throw new ArgumentNullException(nameof(options));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                Initialize();
            }

            private void Initialize()
            {
                if (!_options.Enabled) return;
                if (string.IsNullOrWhiteSpace(_options.FilePath)) return;

                try
                {
                    // FilePath lo tratamos como DIRECTORIO (carpeta de logs)
                    var dir = _options.FilePath;

                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var appName = SanitizeFileName(AppDomain.CurrentDomain.FriendlyName);
                    var date = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss_fff");
                    var fileName = $"Dump-{appName}-{date}.log";
                    var logFilePath = Path.Combine(dir, fileName);

                    // Stream en async + writer UTF8
                    _stream = new FileStream(
                        logFilePath,
                        FileMode.Append,
                        FileAccess.Write,
                        FileShare.Read,
                        bufferSize: 64 * 1024,
                        useAsync: true);

                    _writer = new StreamWriter(_stream, new UTF8Encoding(false), 64 * 1024, leaveOpen: true)
                    {
                        AutoFlush = true
                    };


                // Separador entre sesiones
                _writer.WriteLine($"===== SESSION {DateTimeOffset.Now:O} =====");

                    if (_options.WriteToConsole)
                        _logger.Info($"Logging to: {logFilePath}");
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to create log file:", ex);
                    throw;
                }
            }

            public async ValueTask WriteAsync(string text, CancellationToken ct = default)
            {
                if (!_options.Enabled) return;
                if (_writer is null) return; // si no hay FilePath, no escribe a archivo

                await _gate.WaitAsync(ct);
                try
                {
                    // WriteLineAsync no acepta CancellationToken en netstandard viejo
                    // así que respetamos el ct por el gate y por la cancelación del caller
                    await _writer.WriteLineAsync(text);
                }
                finally
                {
                    _gate.Release();
                }
            }

            private static string SanitizeFileName(string fileName)
            {
                var invalid = Path.GetInvalidFileNameChars();
                return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            }

        public async ValueTask DisposeAsync()
        {
            if (_writer is not null)
            {
                await _writer.FlushAsync();
                _writer.Dispose();
                _writer = null;
            }

            if (_stream is not null)
            {
                await _stream.FlushAsync();
                await _stream.DisposeAsync();
                _stream = null;
            }

            _gate.Dispose();
        }

    }

    public sealed class DumpSink : IAsyncDisposable
        {
            private readonly DumpOptions _opt;
            private readonly DumperFile _dumpFile;
            private readonly Logger _logger;

            public DumpSink(DumpOptions options, Logger logger)
            {
                _opt = options ?? throw new ArgumentNullException(nameof(options));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _dumpFile = new DumperFile(options, logger);
            }

            public async ValueTask WriteAsync(string text, CancellationToken ct = default)
            {
                if (!_opt.Enabled) return;

                if (_opt.WriteToConsole)
                    _logger.Info(text);

                await _dumpFile.WriteAsync(text, ct);
            }

            public async ValueTask DisposeAsync()
            {
                await _dumpFile.DisposeAsync();
                await _logger.DisposeAsync();
            }
        }

}
