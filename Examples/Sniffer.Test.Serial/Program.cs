using SocketIO.Net.Abstractions;
using SocketIO.Net.Diagnostics;
using SocketIO.Net.Transport.Serial;
using System.IO.Ports;
using System.Net;

namespace Sniffer.Test.Serial;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Defaults
        string portName = "COM1";
        int baud = 115200;
        string? outFile = "serial_dump.log";
        int maxBytes = 4096;
        int bytesPerLine = 16;

        // Args: --port COM3 --baud 115200 --out serial.log --max 4096 --bpl 16
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            string? Next() => (i + 1 < args.Length) ? args[++i] : null;

            if (a.Equals("--port", StringComparison.OrdinalIgnoreCase))
            {
                portName = Next() ?? portName;
            }
            else if (a.Equals("--baud", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(Next(), out var v)) baud = v;
            }
            else if (a.Equals("--out", StringComparison.OrdinalIgnoreCase))
            {
                outFile = Next();
            }
            else if (a.Equals("--max", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(Next(), out var v)) maxBytes = v;
            }
            else if (a.Equals("--bpl", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(Next(), out var v)) bytesPerLine = v;
            }
            else if (a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h"))
            {
                PrintHelp();
                return 0;
            }
        }

        var opt = new DumpOptions
        {
            WriteToConsole = true,
            FilePath = outFile,          // null para no escribir archivo
            MaxBytesPerMessage = maxBytes,
            BytesPerLine = bytesPerLine,
        };

        await using var sink = new DumpSink(opt);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        Console.WriteLine($"🕵️ Serial RAW Sniffer");
        Console.WriteLine($"   Port: {portName}  Baud: {baud}");
        Console.WriteLine($"   Log : {(outFile ?? "(none)")}");
        Console.WriteLine("   Ctrl+C para salir\n");

        await using IConnection conn = new SerialConnection(portName, baud);

        // Opcional: dumpear también TX si vos escribís cosas por esta misma conexión.
        // IConnection tapped = new WireTapConnection(conn, sink, opt);
        // Aquí solo RX raw directo.

        var rxBuf = new byte[8192];

        while (!cts.IsCancellationRequested)
        {
            int n;
            try
            {
                n = await conn.ReceiveAsync(rxBuf, cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (n <= 0) continue;

            // ✅ Build texto SYNC con Span, y escribir ASYNC sin Span
            string text = BuildDumpText(opt, "RX", conn.RemoteEndPoint.ToString() ?? "serial?", rxBuf.AsSpan(0, n));
            await sink.WriteAsync(text, cts.Token);
        }

        await conn.CloseAsync();
        Console.WriteLine("\n👋 Sniffer detenido.");
        return 0;
    }

    private static string BuildDumpText(DumpOptions opt, string dir, string remote, ReadOnlySpan<byte> data)
    {
        if (!opt.Enabled) return string.Empty;

        int len = Math.Min(data.Length, opt.MaxBytesPerMessage);
        var slice = data.Slice(0, len);

        var header = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} | {dir} | {remote} | bytes={data.Length}";
        var dump = HexDump.Format(slice, opt.BytesPerLine);

        return header + "\n" + dump;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
                Sniffer.Test.Serial

                Uso:
                  dotnet run -- --port COM3 --baud 115200 --out serial_dump.log --max 4096 --bpl 16

                Args:
                  --port   Nombre del puerto (COM3, /dev/ttyUSB0, etc.)
                  --baud   Baud rate (ej: 9600, 115200)
                  --out    Archivo de salida (null para desactivar)
                  --max    Max bytes por dump
                  --bpl    Bytes por línea (hexdump)
                """);
    }

    // ========= Transport: SerialConnection =========
    private sealed class SerialConnection : IConnection
    {
        private readonly SerialPort _port;

        public SerialConnection(string portName, int baudRate)
        {
            _port = new SerialPort(portName, baudRate)
            {
                ReadTimeout = -1,
                WriteTimeout = -1,
                DtrEnable = true,
                RtsEnable = true
            };

            _port.Open();
        }

        public EndPoint RemoteEndPoint => new SerialEndPoint(_port.PortName, _port.BaudRate);



        public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            await _port.BaseStream.WriteAsync(data, ct);
            await _port.BaseStream.FlushAsync(ct);
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            return await _port.BaseStream.ReadAsync(buffer, ct);
        }

        public ValueTask CloseAsync()
        {
            if (_port.IsOpen) _port.Close();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _port.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
